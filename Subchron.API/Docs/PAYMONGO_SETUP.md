# PayMongo Integration – Setup and Behavior

This document describes how payment attempts are recorded, when subscription access is granted, and what you need to do in the PayMongo dashboard.

---

## 1. What the API Does

### Recording every payment attempt
- **Create intent** (`POST /api/billing/create-intent`): When a payment intent is created, a **PaymentTransaction** row is inserted with:
  - `PayMongoPaymentIntentId`
  - `Status` = normalized from PayMongo (e.g. `pending` for `awaiting_payment_method`)
  - `Amount`, `Currency`, `Description`
  - `OrgID` / `UserID` = null until signup is completed

### Updating status (no duplicates)
- **Webhook** (`POST /api/billing/webhook`): On `payment.paid` or `payment.failed`, the existing transaction is **updated** by `PayMongoPaymentIntentId` (same row, no duplicate).
- **Complete signup** (billing or auth): After server-side verification, the same transaction row is updated with latest status, failure code/message (if any), and optionally `OrgID` / `UserID`.

### Status values stored
- **paid** – Only this status grants subscription access. Mapped from PayMongo `succeeded`.
- **pending** – Awaiting payment or processing. Mapped from `awaiting_payment_method`, `awaiting_next_action`, `processing`.
- **failed** – Payment failed (e.g. denied, insufficient funds). Failure code and message are stored.
- **expired** – Mapped from `cancelled` / `expired`.
- **refunded** – Reserved for future refund handling.

### When subscription access is granted
- **Only when status is confirmed as paid**, via:
  1. **Server-side verification**: `complete-signup` and `complete-signup-with-billing` call PayMongo’s API to get the current payment intent status. If `status != "succeeded"`, the API returns an error and **does not** create the org/user or return login tokens.
  2. **Webhook**: Only updates the database; it does **not** grant access. Access is always gated by the complete-signup endpoints after re-verifying with PayMongo.

So: both successful and unsuccessful outcomes are persisted; only **paid** (PayMongo `succeeded`) allows the user to complete signup and get access.

---

## 2. What You Need to Do

### 2.1 App settings (`appsettings.json`)

```json
{
  "PayMongo": {
    "SecretKey": "sk_test_...",   // or sk_live_... in production
    "PublicKey": "pk_test_...",   // or pk_live_... in production
    "WebhookSecret": ""           // optional; set from PayMongo dashboard (see below)
  }
}
```

- Use **test** keys for development, **live** keys for production.
- **WebhookSecret**: If you set this, the webhook endpoint will verify the request signature and reject invalid requests. Recommended for production.

### 2.2 PayMongo dashboard – Webhook

1. Log in to [PayMongo Dashboard](https://dashboard.paymongo.com).
2. Go to **Developers** → **Webhooks** (or **Settings** → **Webhooks**).
3. **Add endpoint**:
   - **URL**: `https://your-api-domain.com/api/billing/webhook`  
     (must be HTTPS in production; for local testing you can use a tunnel like ngrok).
   - **Events**: Subscribe to at least:
     - `payment.paid`
     - `payment.failed`
4. After saving, copy the **Signing secret** (if provided) into `PayMongo:WebhookSecret` in appsettings.

If PayMongo uses a different signature scheme than HMAC-SHA256 of the raw body, the code in `PayMongoService.VerifyWebhookSignature` may need to be adjusted to match their docs.

### 2.3 Database migrations

Run migrations so `PaymentTransactions` has the audit columns and unique index on `PayMongoPaymentIntentId`:

```bash
cd Subchron.API
dotnet ef database update
```

If you already ran an earlier migration that created `PaymentTransactions`, the migration `PaymentTransactionAuditFields` will add `FailureCode`, `FailureMessage`, `UpdatedAt`, make `OrgID` nullable, and add the unique index.

### 2.4 CORS and HTTPS (production)

- Ensure your API is reachable at HTTPS for the webhook URL.
- If the frontend is on another domain, configure CORS in the API so the billing page can call `create-intent` and `complete-signup`.

---

## 3. Flow Summary

| Step | Action | DB / access |
|------|--------|-------------|
| 1 | User opens billing page → API creates payment intent | Insert **PaymentTransaction** (status e.g. `pending`) |
| 2 | User pays or fails on PayMongo | PayMongo sends **payment.paid** or **payment.failed** → webhook **updates** same transaction |
| 3 | User returns to app → frontend calls **complete-signup** or **complete-signup-with-billing** | API fetches intent from PayMongo; only if status is **succeeded** it returns success and (for draft flow) creates org/user and updates transaction with OrgID/UserID/SubscriptionID |
| 4 | If payment was failed/expired | Transaction remains failed/expired; complete-signup returns error and does **not** create org or grant access |

---

## 4. Traceability and auditing

Each **PaymentTransaction** row includes:

- `PayMongoPaymentIntentId` – links to PayMongo.
- `PayMongoPaymentId` – set when a payment exists (e.g. from webhook or GetPaymentIntent).
- `Status` – normalized status (paid, pending, failed, expired, refunded).
- `FailureCode` / `FailureMessage` – from PayMongo when available (e.g. card denied, insufficient funds).
- `OrgID` / `UserID` / `SubscriptionID` – set after signup completion for paid flows.

You can use these for support, reconciliation, and super admin sales/reporting.
