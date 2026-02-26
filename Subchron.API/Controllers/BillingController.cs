using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Services;
using Subchron.API.Models.Auth;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly JwtTokenService _jwt;
    private readonly PayMongoService _payMongo;
    private readonly SubchronDbContext _db;
    private readonly EmailService _email;

    public BillingController(JwtTokenService jwt, PayMongoService payMongo, SubchronDbContext db, EmailService email)
    {
        _jwt = jwt;
        _payMongo = payMongo;
        _db = db;
        _email = email;
    }

    // Returns current organization's plan and usage (employees used/limit). Requires JWT with orgId (OrgAdmin/HR/Manager).
    [Authorize]
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage()
    {
        var orgIdClaim = User.FindFirstValue("orgId");
        if (string.IsNullOrEmpty(orgIdClaim) || !int.TryParse(orgIdClaim, out var orgId))
            return Ok(new { planName = "—", employeesUsed = 0, employeeLimit = 0, billingCycle = "—", nextBillingDate = "—" });

        var subscription = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.OrgID == orgId && (s.Status == "Active" || s.Status == "Trial"))
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        var plan = subscription?.Plan;
        var planName = plan?.PlanName ?? "—";
        var employeeLimit = plan?.MaxEmployees ?? 0;
        var employeesUsed = await _db.Employees.CountAsync(e => e.OrgID == orgId);
        var billingCycle = subscription?.BillingCycle ?? "—";
        var nextBillingDate = subscription?.EndDate.HasValue == true ? subscription.EndDate!.Value.ToString("yyyy-MM-dd") : "—";
        var trialEndDate = subscription?.EndDate.HasValue == true ? subscription.EndDate!.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : (string?)null;
        var trialExpired = subscription != null && subscription.Status == "Trial" && subscription.EndDate.HasValue && subscription.EndDate.Value < DateTime.UtcNow;

        return Ok(new
        {
            planName,
            employeesUsed,
            employeeLimit,
            billingCycle,
            nextBillingDate,
            trialExpired,
            trialEndDate
        });
    }

    [Authorize]
    [HttpPost("create-upgrade-token")]
    public async Task<IActionResult> CreateUpgradeToken()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orgIdClaim = User.FindFirstValue("orgId");
        var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(orgIdClaim) || !int.TryParse(orgIdClaim, out var orgId) || !int.TryParse(userIdClaim, out var userId))
            return BadRequest(new { ok = false, message = "Invalid session." });
        var sub = await _db.Subscriptions.Include(s => s.Plan).Where(s => s.OrgID == orgId && s.Status == "Trial").OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();
        if (sub?.Plan == null) return BadRequest(new { ok = false, message = "No trial subscription found." });
        var planName = sub.Plan.PlanName ?? "";
        var token = _jwt.CreateOnboardingToken(userId, orgId, roleClaim ?? "OrgAdmin", sub.PlanID, planName, false);
        return Ok(new { ok = true, token });
    }

    [Authorize]
    [HttpPost("cancel-subscription")]
    public async Task<IActionResult> CancelSubscription()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orgIdClaim = User.FindFirstValue("orgId");
        if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(orgIdClaim) ||
            !int.TryParse(userIdClaim, out var userId) || !int.TryParse(orgIdClaim, out var orgId))
            return BadRequest(new { ok = false, message = "Invalid session." });

        var sub = await _db.Subscriptions
            .Where(s => s.OrgID == orgId && (s.Status == "Trial" || s.Status == "Active"))
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (sub == null)
            return BadRequest(new { ok = false, message = "No active subscription found." });

        sub.Status = "Cancelled";
        if (!sub.EndDate.HasValue || sub.EndDate.Value > DateTime.UtcNow)
            sub.EndDate = DateTime.UtcNow;

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.OrgID == orgId);
        if (org != null)
            org.Status = "Cancelled";

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            OrgID = orgId,
            UserID = userId,
            Action = "SubscriptionCancelled",
            EntityName = "Subscription",
            EntityID = sub.SubscriptionID,
            Details = "Subscription cancelled by user from trial-expired modal.",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, status = "Cancelled" });
    }

    [HttpGet("summary")]
    public IActionResult GetSummary([FromQuery] decimal amount, [FromQuery] bool isFreeTrial = false, [FromQuery] string? planName = null)
    {
        var summary = BillingSummaryHelper.GetSummary(amount, isFreeTrial, planName ?? "Subscription");
        return Ok(summary);
    }

    // Paid plans: create PayMongo intent and return client key; free trial: just return isFreeTrial and planName. planId can override token's plan.
    [HttpPost("create-intent")]
    public async Task<IActionResult> CreateIntent([FromBody] CreateIntentRequest req)
    {
        var token = (req.OnboardingToken ?? req.SignupToken ?? req.DraftToken ?? "").Trim();
        if (string.IsNullOrEmpty(token))
            return BadRequest(new { ok = false, message = "Missing token. Please complete signup first." });

        string planName;
        bool isFreeTrial;
        bool isDraft = false;
        bool freeTrialEligible = false;

        var payload = _jwt.ValidateOnboardingToken(token);
        if (payload != null)
        {
            planName = payload.PlanName;
            isFreeTrial = payload.IsFreeTrial;
            // Existing accounts coming from onboarding/upgrade tokens are not eligible
            // to restart a free trial.
            freeTrialEligible = false;
        }
        else
        {
            var draft = SignupDraftStore.Get(token);
            if (draft == null)
                return BadRequest(new { ok = false, message = "Invalid or expired session. Please sign up again." });
            isDraft = true;
            var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanID == draft.PlanId && p.IsActive);
            if (plan is null)
                return BadRequest(new { ok = false, message = "Invalid plan." });
            planName = plan.PlanName ?? "";
            freeTrialEligible = true;
            isFreeTrial = freeTrialEligible && planName == "Standard";
        }

        // Never allow free-trial flag when session is not eligible.
        if (!freeTrialEligible)
            isFreeTrial = false;

        if (req.PlanId.HasValue && req.PlanId.Value > 0)
        {
            var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanID == req.PlanId.Value && p.IsActive);
            if (plan == null)
                return BadRequest(new { ok = false, message = "Invalid plan selected." });
            planName = plan.PlanName ?? "";
            isFreeTrial = freeTrialEligible && planName == "Standard";
        }

        string? billingEmail = null;
        string? billingPhone = null;
        string? nameOnCard = null;
        string? brand = null;
        string? expiry = null;
        string? last4 = null;
        string preferredMethod = "card";

        if (payload != null)
        {
            var latestBilling = await _db.BillingRecords
                .AsNoTracking()
                .Where(b => b.OrgID == payload.OrgId && b.UserID == payload.UserId)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestBilling != null)
            {
                billingEmail = latestBilling.BillingEmail;
                billingPhone = latestBilling.BillingPhone;
                nameOnCard = latestBilling.NameOnCard;
                brand = latestBilling.Brand;
                expiry = latestBilling.Expiry;
                last4 = latestBilling.Last4;

                if (!string.IsNullOrWhiteSpace(brand))
                {
                    var b = brand.Trim().ToLowerInvariant();
                    preferredMethod = b switch
                    {
                        "gcash" => "gcash",
                        "paymaya" => "paymaya",
                        "maya" => "paymaya",
                        _ => "card"
                    };
                }
            }

            if (string.IsNullOrWhiteSpace(billingEmail))
            {
                var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == payload.UserId);
                billingEmail = user?.Email;
            }
        }

        if (isDraft)
        {
            var draft = SignupDraftStore.Get(token);
            billingEmail = draft?.AdminEmail;
        }

        if (isFreeTrial)
        {
            return Ok(new
            {
                ok = true,
                isFreeTrial = true,
                planName,
                amount = 0m,
                clientKey = (string?)null,
                paymentIntentId = (string?)null,
                isDraft,
                billingEmail,
                billingPhone,
                nameOnCard,
                brand,
                expiry,
                last4,
                preferredMethod,
                freeTrialEligible
            });
        }

        var amountPesos = GetDisplayPrice(planName);
        if (amountPesos <= 0)
            return BadRequest(new { ok = false, message = "Invalid plan for payment." });

        try
        {
            var result = await _payMongo.CreatePaymentIntentAsync(amountPesos, "PHP", "Subchron - " + planName);
            if (result == null)
                return StatusCode(500, new { ok = false, message = "Could not create payment session." });
            var amountCentavos = result.Amount;
            var amountDecimal = amountCentavos / 100m;
            await UpsertPaymentTransactionAsync(
                payMongoPaymentIntentId: result.Id,
                status: MapPayMongoStatusToOurStatus(result.Status),
                amount: amountDecimal,
                payMongoPaymentId: result.PayMongoPaymentId,
                failureCode: result.LastPaymentErrorCode,
                failureMessage: result.LastPaymentErrorMessage,
                description: "Subchron - " + planName
            );
            return Ok(new
            {
                ok = true,
                isFreeTrial = false,
                planName,
                amount = amountPesos,
                clientKey = result.ClientKey,
                paymentIntentId = result.Id,
                isDraft,
                billingEmail,
                billingPhone,
                nameOnCard,
                brand,
                expiry,
                last4,
                preferredMethod,
                freeTrialEligible
            });
        }
        catch (InvalidOperationException ex)
        {
            var msg = ex.Message.StartsWith("PayMongo:", StringComparison.OrdinalIgnoreCase)
                ? ex.Message
                : "Could not create payment session. Check PayMongo keys in appsettings.";
            return StatusCode(502, new { ok = false, message = msg });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, message = "Could not create payment session. " + (ex.Message ?? "") });
        }
    }

    // Finishes onboarding only when payment status is confirmed paid (server-side). Updates transaction with org/user.
    [HttpPost("complete-signup")]
    public async Task<IActionResult> CompleteSignup([FromBody] CompleteSignupRequest req)
    {
        var token = req.OnboardingToken ?? req.SignupToken;
        var payload = _jwt.ValidateOnboardingToken(token);
        if (payload == null)
            return BadRequest(new { ok = false, message = "Invalid or expired session. Please sign up again." });

        if (payload.IsFreeTrial)
        {
            if (!string.IsNullOrWhiteSpace(req.PaymentIntentId))
                return BadRequest(new { ok = false, message = "Free trial does not require payment." });
            var freeUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == payload.UserId);
            var name = freeUser?.Name;
            var accessToken = freeUser != null ? _jwt.CreateToken(freeUser, null) : null;
            return Ok(new { ok = true, userId = payload.UserId, orgId = payload.OrgId, role = payload.Role, name, token = accessToken });
        }

        if (string.IsNullOrWhiteSpace(req.PaymentIntentId))
            return BadRequest(new { ok = false, message = "Payment is required for this plan." });

        // Server-side verification: fetch current status from PayMongo
        var intent = await _payMongo.GetPaymentIntentAsync(req.PaymentIntentId);
        if (intent == null)
            return BadRequest(new { ok = false, message = "Payment session not found." });
        var ourStatus = MapPayMongoStatusToOurStatus(intent.Status);
        if (ourStatus != PaymentStatus.Paid)
            return BadRequest(new { ok = false, message = "Payment has not been completed. Only confirmed paid payments grant access." });

        // Update or ensure payment transaction record (idempotent)
        await UpsertPaymentTransactionAsync(
            payMongoPaymentIntentId: req.PaymentIntentId,
            status: PaymentStatus.Paid,
            amount: intent.Amount / 100m,
            payMongoPaymentId: intent.PayMongoPaymentId,
            failureCode: intent.LastPaymentErrorCode,
            failureMessage: intent.LastPaymentErrorMessage,
            description: null,
            orgId: payload.OrgId,
            userId: payload.UserId
        );

        if (payload.OrgId > 0 && payload.UserId > 0)
        {
            var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.PayMongoPaymentIntentId == req.PaymentIntentId);
            if (tx != null)
                await UpsertBillingRecordAsync(tx.Id, payload.OrgId, payload.UserId, req);
            var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.OrgID == payload.OrgId && s.Status == "Trial");
            if (sub != null)
            {
                sub.Status = "Active";
                sub.EndDate = DateTime.UtcNow.AddMonths(1);
                await _db.SaveChangesAsync();
            }
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == payload.UserId);
        var userName = user?.Name;
        var jwtToken = user != null ? _jwt.CreateToken(user, null) : null;
        var to = (req.BillingEmail ?? user?.Email)?.Trim();
        if (!string.IsNullOrEmpty(to))
        {
            var amountPesos = intent.Amount / 100m;
            var amountLine = "₱" + amountPesos.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("en-PH"));
            try { _ = _email.SendAsync(to, "Your Subchron receipt", EmailTemplates.GetReceiptHtml(payload.PlanName ?? "Subscription", amountLine, false, to)); }
            catch { /* best effort */ }
        }
        return Ok(new { ok = true, userId = payload.UserId, orgId = payload.OrgId, role = payload.Role, name = userName, token = jwtToken });
    }

    // PayMongo webhook: payment.paid / payment.failed. Update transaction by payment_intent_id (no duplicates).
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var body = await reader.ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
            return BadRequest();

        var signature = HttpContext.Request.Headers["Paymongo-Signature"].FirstOrDefault() ?? "";
        if (!_payMongo.VerifyWebhookSignature(body, signature))
            return Unauthorized();

        try
        {
            using var doc = JsonDocument.Parse(body);
            var data = doc.RootElement.GetProperty("data");
            var attrs = data.GetProperty("attributes");
            var eventType = attrs.GetProperty("type").GetString();
            if (string.IsNullOrEmpty(eventType) || (!eventType.Equals("payment.paid", StringComparison.OrdinalIgnoreCase) && !eventType.Equals("payment.failed", StringComparison.OrdinalIgnoreCase)))
                return Ok();

            var inner = attrs.GetProperty("data");
            var paymentId = inner.GetProperty("id").GetString();
            var innerAttrs = inner.GetProperty("attributes");
            var paymentIntentId = innerAttrs.TryGetProperty("payment_intent_id", out var pi) ? pi.GetString() : null;
            if (string.IsNullOrEmpty(paymentIntentId)) return Ok();
            var amount = innerAttrs.TryGetProperty("amount", out var am) ? am.GetInt32() : 0;
            var status = innerAttrs.TryGetProperty("status", out var st) ? st.GetString() : null;

            string? failureCode = null, failureMessage = null;
            if (innerAttrs.TryGetProperty("last_payment_error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                if (err.TryGetProperty("code", out var ec)) failureCode = ec.GetString();
                if (err.TryGetProperty("message", out var em)) failureMessage = em.GetString();
            }
            if (string.IsNullOrEmpty(failureMessage) && innerAttrs.TryGetProperty("failure_message", out var fm))
                failureMessage = fm.GetString();

            var ourStatus = eventType.Equals("payment.paid", StringComparison.OrdinalIgnoreCase) ? PaymentStatus.Paid : PaymentStatus.Failed;
            await UpsertPaymentTransactionAsync(
                payMongoPaymentIntentId: paymentIntentId!,
                status: ourStatus,
                amount: amount / 100m,
                payMongoPaymentId: paymentId,
                failureCode: failureCode,
                failureMessage: failureMessage,
                description: null
            );
        }
        catch (JsonException) { /* accept but don't update */ }

        return Ok();
    }

    private static decimal GetDisplayPrice(string planName)
    {
        return planName switch
        {
            "Basic" => 2499m,
            "Standard" => 5999m,
            "Enterprise" => 8999m,
            _ => 0m
        };
    }

    // Maps PayMongo payment_intent status to our normalized status. Only "paid" grants access.
    private static string MapPayMongoStatusToOurStatus(string? payMongoStatus)
    {
        if (string.IsNullOrEmpty(payMongoStatus)) return PaymentStatus.Pending;
        return payMongoStatus.ToLowerInvariant() switch
        {
            "succeeded" => PaymentStatus.Paid,
            "processing" or "awaiting_next_action" or "awaiting_payment_method" => PaymentStatus.Pending,
            "cancelled" or "expired" => PaymentStatus.Expired,
            "failed" => PaymentStatus.Failed,
            _ => PaymentStatus.Pending
        };
    }

    // Find by PayMongoPaymentIntentId and update; or insert if not found. Ensures one record per payment intent.
    private async Task UpsertPaymentTransactionAsync(
        string payMongoPaymentIntentId,
        string status,
        decimal amount,
        string? payMongoPaymentId = null,
        string? failureCode = null,
        string? failureMessage = null,
        string? description = null,
        int? orgId = null,
        int? userId = null)
    {
        var tx = await _db.PaymentTransactions
            .FirstOrDefaultAsync(t => t.PayMongoPaymentIntentId == payMongoPaymentIntentId);
        var now = DateTime.UtcNow;
        if (tx != null)
        {
            tx.Status = status;
            tx.Amount = amount;
            tx.UpdatedAt = now;
            if (payMongoPaymentId != null) tx.PayMongoPaymentId = payMongoPaymentId.Length > 100 ? payMongoPaymentId.Substring(0, 100) : payMongoPaymentId;
            if (failureCode != null) tx.FailureCode = failureCode.Length > 50 ? failureCode.Substring(0, 50) : failureCode;
            if (failureMessage != null) tx.FailureMessage = failureMessage.Length > 200 ? failureMessage.Substring(0, 200) : failureMessage;
            if (description != null) tx.Description = description.Length > 200 ? description.Substring(0, 200) : description;
            if (orgId.HasValue) tx.OrgID = orgId;
            if (userId.HasValue) tx.UserID = userId;
        }
        else
        {
            _db.PaymentTransactions.Add(new PaymentTransaction
            {
                PayMongoPaymentIntentId = payMongoPaymentIntentId,
                Status = status,
                Amount = amount,
                Currency = "PHP",
                PayMongoPaymentId = payMongoPaymentId?.Length > 100 ? payMongoPaymentId.Substring(0, 100) : payMongoPaymentId,
                FailureCode = failureCode?.Length > 50 ? failureCode.Substring(0, 50) : failureCode,
                FailureMessage = failureMessage?.Length > 200 ? failureMessage.Substring(0, 200) : failureMessage,
                Description = description?.Length > 200 ? description.Substring(0, 200) : description,
                OrgID = orgId,
                UserID = userId,
                CreatedAt = now
            });
        }
        await _db.SaveChangesAsync();
    }

    private static string? NormalizeBillingPhoneTo11Digits(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;
        if (digits.Length >= 11) return digits.Length > 11 ? digits.Substring(digits.Length - 11, 11) : digits;
        if (digits.StartsWith("63") && digits.Length >= 10) return "0" + digits.Substring(2, Math.Min(9, digits.Length - 2)).PadRight(9, '0');
        if (digits.StartsWith("1") && digits.Length >= 10) return digits.Substring(0, 11);
        return digits.PadLeft(11, '0').Substring(0, 11);
    }

    private async Task UpsertBillingRecordAsync(int paymentTransactionId, int orgId, int userId, CompleteSignupRequest req)
    {
        var phone = NormalizeBillingPhoneTo11Digits(req.BillingPhone);
        var existing = await _db.BillingRecords.FirstOrDefaultAsync(b => b.PaymentTransactionId == paymentTransactionId);
        var nameOnCard = req.NameOnCard?.Trim();
        if (nameOnCard != null && nameOnCard.Length > 100) nameOnCard = nameOnCard.Substring(0, 100);
        var email = req.BillingEmail?.Trim();
        if (email != null && email.Length > 256) email = email.Substring(0, 256);
        var last4 = req.Last4?.Trim();
        if (last4 != null && last4.Length > 4) last4 = last4.Substring(0, 4);
        var expiry = req.Expiry?.Trim();
        if (expiry != null && expiry.Length > 5) expiry = expiry.Substring(0, 5);
        var brand = req.Brand?.Trim();
        if (brand != null && brand.Length > 20) brand = brand.Substring(0, 20);

        if (existing != null)
        {
            existing.BillingEmail = email;
            existing.BillingPhone = phone;
            existing.NameOnCard = nameOnCard;
            existing.Last4 = last4;
            existing.Expiry = expiry;
            existing.Brand = brand;
        }
        else
        {
            _db.BillingRecords.Add(new BillingRecord
            {
                OrgID = orgId,
                UserID = userId,
                PaymentTransactionId = paymentTransactionId,
                BillingEmail = email,
                BillingPhone = phone,
                NameOnCard = nameOnCard,
                Last4 = last4,
                Expiry = expiry,
                Brand = brand,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }
}

public static class PaymentStatus
{
    public const string Paid = "paid";
    public const string Pending = "pending";
    public const string Failed = "failed";
    public const string Expired = "expired";
    public const string Refunded = "refunded";
}

public class CreateIntentRequest
{
    [JsonPropertyName("onboardingToken")]
    public string? OnboardingToken { get; set; }
    [JsonPropertyName("signupToken")]
    public string? SignupToken { get; set; }
    [JsonPropertyName("draftToken")]
    public string? DraftToken { get; set; }
    [JsonPropertyName("planId")]
    public int? PlanId { get; set; }
}

public class CompleteSignupRequest
{
    [JsonPropertyName("onboardingToken")]
    public string? OnboardingToken { get; set; }
    [JsonPropertyName("signupToken")]
    public string? SignupToken { get; set; }
    [JsonPropertyName("paymentIntentId")]
    public string? PaymentIntentId { get; set; }
    [JsonPropertyName("billingEmail")]
    public string? BillingEmail { get; set; }
    [JsonPropertyName("billingPhone")]
    public string? BillingPhone { get; set; }
    [JsonPropertyName("nameOnCard")]
    public string? NameOnCard { get; set; }
    [JsonPropertyName("last4")]
    public string? Last4 { get; set; }
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }
}
