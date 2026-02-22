namespace Subchron.API.Services;

public static class EmailTemplates
{
    private const string LockIconUrl = "https://i.ibb.co/zhstnPv1/lock-icon.png";
    private const string AppName = "Subchron";
    private const string SupportEmail = "support@subchron.com";
    private const string TermsUrl = "https://subchron.com/terms";
    private const string PrivacyUrl = "https://subchron.com/privacy";


    public static string GetPasswordResetHtml(string resetLink, string userEmail, string? logoUrl = null, string? webBaseUrl = null)
    {
var year = DateTime.UtcNow.Year.ToString();
        
        // Build terms and privacy URLs from webBaseUrl if provided
        var termsLink = webBaseUrl != null ? $"{webBaseUrl.TrimEnd('/')}/terms" : TermsUrl;
        var privacyLink = webBaseUrl != null ? $"{webBaseUrl.TrimEnd('/')}/privacy" : PrivacyUrl;

        return $@"
<!doctype html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"" xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"" />
  <meta name=""x-apple-disable-message-reformatting"" />
  <meta name=""format-detection"" content=""telephone=no,address=no,email=no,date=no,url=no"" />
  <title>Reset your password - {AppName}</title>

  <!--[if mso]>
  <xml>
    <o:OfficeDocumentSettings>
      <o:AllowPNG/>
      <o:PixelsPerInch>96</o:PixelsPerInch>
    </o:OfficeDocumentSettings>
  </xml>
  <![endif]-->

  <style>
    html, body {{ margin:0 !important; padding:0 !important; height:100% !important; width:100% !important; }}
    table, td {{ border-collapse:collapse !important; }}
    img {{ border:0; outline:none; text-decoration:none; -ms-interpolation-mode:bicubic; }}
    a {{ text-decoration:none; }}

    @media (max-width: 520px) {{
      .wrap {{ width:100% !important; }}
      .px {{ padding-left:16px !important; padding-right:16px !important; }}
      .card {{ width:100% !important; }}
    }}
  </style>
</head>

<body style=""margin:0; padding:0; background:#f5f7fb;"">
  <!-- Preheader -->
  <div style=""display:none; max-height:0; overflow:hidden; opacity:0; mso-hide:all;"">
    Reset your password for your {AppName} account.
  </div>

  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""width:100%; background:#f5f7fb;"">
    <tr>
      <td align=""center"" style=""padding:32px 16px 40px;"">

        <!-- Outer wrapper -->
        <table role=""presentation"" class=""wrap"" width=""480"" cellspacing=""0"" cellpadding=""0"" border=""0""
     style=""width:480px; max-width:480px;
           background:
         radial-gradient(200px 200px at 10% 15%, rgba(34,197,94,.08), rgba(34,197,94,0) 70%),
    radial-gradient(240px 240px at 90% 80%, rgba(16,185,129,.06), rgba(16,185,129,0) 70%),
   #f5f7fb;
       border-radius:16px;"">
          <tr>
  <td class=""px"" style=""padding:24px 20px;"">

  <!-- Logo -->
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
        <tr>
          <td align=""center"" style=""padding:0 0 20px;"">
 <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
  font-size:22px; line-height:22px; font-weight:800; letter-spacing:.2px;"">
          <span style=""color:#22c55e;"">Sub</span><span style=""color:#0052a3;"">chron</span>
       </div>
              </td>
        </tr>
     </table>

          <!-- Card -->
        <table role=""presentation"" class=""card"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""
       style=""width:100%; max-width:440px; margin:0 auto;
         background:#ffffff;
       border:1px solid #e2e8f0;
          border-radius:14px;
            box-shadow:0 4px 16px rgba(15,23,42,.06);"">
<tr>
            <td align=""center"" style=""padding:32px 28px 28px;"">

             <!-- Lock icon image -->
         <img src=""{LockIconUrl}"" alt=""Reset Password"" width=""56"" height=""56""
    style=""display:block; border:0; outline:none;"" />

            <div style=""height:20px;""></div>

           <!-- Title -->
           <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
    font-size:20px; line-height:26px; font-weight:700; color:#0f172a;"">
        Reset your password
  </div>

     <div style=""height:12px;""></div>

                <!-- Body -->
             <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
  font-size:14px; line-height:21px; color:#64748b;"">
      We received a request to reset the password for your {AppName} account associated with
        </div>

          <div style=""height:4px;""></div>

<div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
       font-size:14px; line-height:21px; color:#334155; font-weight:600;"">
        {userEmail}
           </div>

     <div style=""height:24px;""></div>

              <!-- Button -->
           <!--[if mso]>
          <v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word""
  href=""{resetLink}""
       style=""height:46px;v-text-anchor:middle;width:180px;"" arcsize=""12%"" stroke=""f"" fillcolor=""#16a34a"">
                <w:anchorlock/>
             <center style=""color:#ffffff;font-family:Arial,sans-serif;font-size:14px;font-weight:700;"">
    Reset Password
</center>
</v:roundrect>
  <![endif]-->
        <!--[if !mso]><!-->
  <a href=""{resetLink}""
           style=""display:inline-block;
        background:#16a34a; color:#ffffff;
 font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
 font-size:14px; line-height:46px; height:46px; font-weight:600;
     padding:0 32px; border-radius:8px;"">
    Reset Password
  </a>
   <!--<![endif]-->

             <div style=""height:24px;""></div>

       <!-- Divider -->
 <div style=""width:100%; height:1px; background:#e2e8f0;""></div>

        <div style=""height:20px;""></div>

       <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
           font-size:12px; line-height:18px; color:#94a3b8;"">
              If the button doesn't work, copy and paste this link in your browser
         </div>

   <div style=""height:8px;""></div>

            <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
 font-size:11px; line-height:16px; word-break:break-all;"">
    <a href=""{resetLink}"" style=""color:#16a34a; font-weight:500;"">
      {resetLink}
           </a>
        </div>

            <div style=""height:20px;""></div>

        <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
         font-size:12px; line-height:18px; color:#64748b;"">
          If you didn't request a password reset, you can safely ignore this email.
       </div>

          <div style=""height:20px;""></div>

     <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
           font-size:13px; line-height:20px; color:#334155;"">
         Thank you,<br/>
             <span style=""font-weight:700;"">The {AppName} Team</span>
</div>

          </td>
         </tr>
              </table>

        <!-- Footer below card -->
      <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""
             style=""max-width:440px; margin:20px auto 0;"">
  <tr>
         <td align=""center"" style=""padding:8px 12px 0;"">

   <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
         font-size:11px; line-height:17px; color:#94a3b8;"">
       © {year} {AppName}. All rights reserved.
        </div>

    <div style=""height:8px;""></div>

     <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
      font-size:11px; line-height:17px; color:#94a3b8;"">
        Need help? Contact us at
      <a href=""mailto:{SupportEmail}"" style=""color:#16a34a; font-weight:600;"">{SupportEmail}</a>
            </div>

  <div style=""height:8px;""></div>

          <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
       font-size:11px; line-height:17px;"">
     <a href=""{termsLink}"" style=""color:#64748b; font-weight:500;"">Terms</a>
       <span style=""color:#cbd5e1; margin:0 8px;"">•</span>
        <a href=""{privacyLink}"" style=""color:#64748b; font-weight:500;"">Privacy</a>
          </div>

        </td>
           </tr>
   </table>

            </td>
          </tr>
      </table>

      </td>
    </tr>
  </table>
</body>
</html>";
  }

    public static string GetPasswordResetPlainText(string resetLink, string userEmail)
    {
var year = DateTime.UtcNow.Year.ToString();

        return $@"
RESET YOUR PASSWORD
===================

Hi there,

We received a request to reset the password for your {AppName} account associated with {userEmail}.

Click the link below to reset your password:
{resetLink}

---

If the link doesn't work, copy and paste the URL into your browser.

---

If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.

Thank you,
The {AppName} Team

---

© {year} {AppName}. All rights reserved.
Need help? Contact us at {SupportEmail}

Terms: {TermsUrl}
Privacy: {PrivacyUrl}
";
    }

    public static string GetPasswordResetTemplateWithPlaceholders()
    {
     return @"
<!doctype html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"" xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"" />
  <meta name=""x-apple-disable-message-reformatting"" />
  <title>Reset your password - {{APP_NAME}}</title>

  <!--[if mso]>
  <xml>
    <o:OfficeDocumentSettings>
      <o:AllowPNG/>
      <o:PixelsPerInch>96</o:PixelsPerInch>
    </o:OfficeDocumentSettings>
  </xml>
  <![endif]-->

  <style>
    html, body { margin:0 !important; padding:0 !important; }
table, td { border-collapse:collapse !important; }
    img { border:0; outline:none; text-decoration:none; }
    a { text-decoration:none; }
@media (max-width: 520px) {
      .wrap { width:100% !important; }
      .px { padding-left:16px !important; padding-right:16px !important; }
      .card { width:100% !important; }
    }
  </style>
</head>

<body style=""margin:0; padding:0; background:#f5f7fb;"">
  <div style=""display:none; max-height:0; overflow:hidden;"">
    Reset your password for your {{APP_NAME}} account.
  </div>

  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background:#f5f7fb;"">
    <tr>
  <td align=""center"" style=""padding:32px 16px 40px;"">

        <table role=""presentation"" class=""wrap"" width=""480"" cellspacing=""0"" cellpadding=""0"" border=""0""
   style=""width:480px; max-width:480px; background:#f5f7fb; border-radius:16px;"">
          <tr>
            <td class=""px"" style=""padding:24px 20px;"">

            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
           <tr>
                  <td align=""center"" style=""padding:0 0 20px;"">
                <div style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
      font-size:22px; font-weight:800;"">
           <span style=""color:#22c55e;"">Sub</span><span style=""color:#0052a3;"">chron</span>
         </div>
        </td>
   </tr>
        </table>

              <table role=""presentation"" class=""card"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""
         style=""max-width:440px; margin:0 auto; background:#ffffff; border:1px solid #e2e8f0; border-radius:14px;"">
      <tr>
          <td align=""center"" style=""padding:32px 28px 28px;"">

 <img src=""{{LOCK_ICON_URL}}"" alt=""Reset Password"" width=""56"" height=""56"" style=""display:block;"" />

       <div style=""height:20px;""></div>

        <div style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
          font-size:20px; font-weight:700; color:#0f172a;"">
          Reset your password
      </div>

    <div style=""height:12px;""></div>

   <div style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
      font-size:14px; color:#64748b;"">
       We received a request to reset the password for your {{APP_NAME}} account associated with
   </div>

            <div style=""height:4px;""></div>

    <div style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
           font-size:14px; color:#334155; font-weight:600;"">
{{USER_EMAIL}}
</div>

     <div style=""height:24px;""></div>

  <a href=""{{RESET_LINK}}""
style=""display:inline-block; background:#16a34a; color:#ffffff;
  font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
              font-size:14px; line-height:46px; font-weight:600; padding:0 32px; border-radius:8px;"">
      Reset Password
       </a>

   <div style=""height:24px;""></div>

       <!-- Divider -->
 <div style=""width:100%; height:1px; background:#e2e8f0;""></div>

        <div style=""height:20px;""></div>

       <div style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
           font-size:12px; color:#94a3b8;"">
              If the button doesn't work, copy and paste this link in your browser
         </div>

   <div style=""height:8px;""></div>

            <div style=""font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
 font-size:11px; line-height:16px; word-break:break-all;"">
    <a href=""{{RESET_LINK}}"" style=""color:#16a34a; font-weight:500;"">{{RESET_LINK}}</a>
        </div>

            <div style=""height:20px;""></div>

        <div style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
         font-size:12px; color:#64748b;"">
          If you didn't request a password reset, you can safely ignore this email.
       </div>

          <div style=""height:20px;""></div>

        <div style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
      font-size:13px; color:#334155;"">
            Thank you,<br/><span style=""font-weight:700;"">The {{APP_NAME}} Team</span>
           </div>

            </td>
            </tr>
            </table>

   <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""
         style=""max-width:440px; margin:20px auto 0;"">
    <tr>
        <td align=""center"" style=""padding:8px 12px 0;"">
            <div style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
    font-size:11px; color:#94a3b8;"">
     © {{YEAR}} {{APP_NAME}}. All rights reserved.
          </div>
    <div style=""height:8px;""></div>
        <div style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
    font-size:11px; color:#94a3b8;"">
         Need help? <a href=""mailto:{{SUPPORT_EMAIL}}"" style=""color:#16a34a; font-weight:600;"">{{SUPPORT_EMAIL}}</a>
   </div>
   <div style=""height:8px;""></div>
    <div style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;
      font-size:11px;"">
            <a href=""{{TERMS_URL}}"" style=""color:#64748b; font-weight:500;"">Terms</a>
       <span style=""color:#cbd5e1; margin:0 8px;"">•</span>
    <a href=""{{PRIVACY_URL}}"" style=""color:#64748b; font-weight:500;"">Privacy</a>
         </div>
   </td>
       </tr>
     </table>

            </td>
  </tr>
    </table>

      </td>
    </tr>
  </table>
</body>
</html>";
    }

    public static string GetPasswordResetPlainTextWithPlaceholders()
 {
        return @"
RESET YOUR PASSWORD
===================

Hi there,

We received a request to reset the password for your {{APP_NAME}} account associated with {{USER_EMAIL}}.

Click the link below to reset your password:
{{RESET_LINK}}

---

If the link doesn't work, copy and paste the URL into your browser.

---

If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.

Thank you,
The {{APP_NAME}} Team

---

© {{YEAR}} {{APP_NAME}}. All rights reserved.
Need help? Contact us at {{SUPPORT_EMAIL}}

Terms: {{TERMS_URL}}
Privacy: {{PRIVACY_URL}}
";
    }

    // Builds the verification-code email body for the signup flow.
    public static string GetVerificationCodeHtml(string verificationCode, string userEmail, string? webBaseUrl = null)
    {
        var year = DateTime.UtcNow.Year.ToString();
        return $@"
<!doctype html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"" />
  <title>Verify your email - {AppName}</title>
  <style>
    html, body {{ margin:0 !important; padding:0 !important; height:100% !important; width:100% !important; }}
    table, td {{ border-collapse:collapse !important; }}
    body {{ background:#f5f7fb; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif; }}
    .code {{ font-size:28px; font-weight:700; letter-spacing:8px; color:#0f172a; background:#f1f5f9; padding:16px 24px; border-radius:8px; display:inline-block; margin:16px 0; }}
  </style>
</head>
<body style=""margin:0; padding:0; background:#f5f7fb;"">
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""width:100%; background:#f5f7fb;"">
    <tr>
      <td align=""center"" style=""padding:32px 16px 40px;"">
        <table role=""presentation"" width=""480"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""width:480px; max-width:100%; background:#fff; border:1px solid #e2e8f0; border-radius:14px; box-shadow:0 4px 16px rgba(15,23,42,.06);"">
          <tr>
            <td style=""padding:32px 28px;"">
              <div style=""font-size:22px; font-weight:800;"">
                <span style=""color:#22c55e;"">Sub</span><span style=""color:#0052a3;"">chron</span>
              </div>
              <div style=""height:24px;""></div>
              <div style=""font-size:20px; font-weight:700; color:#0f172a;"">Verify your email</div>
              <div style=""height:12px;""></div>
              <div style=""font-size:14px; color:#64748b;"">
                Use the code below to continue setting up your account for
              </div>
              <div style=""font-size:14px; color:#334155; font-weight:600; margin-top:4px;"">{userEmail}</div>
              <div style=""height:8px;""></div>
              <div class=""code"" style=""font-size:28px; font-weight:700; letter-spacing:8px; color:#0f172a; background:#f1f5f9; padding:16px 24px; border-radius:8px; display:inline-block; margin:16px 0;"">{verificationCode}</div>
              <div style=""font-size:12px; color:#94a3b8;"">This code expires in 15 minutes.</div>
              <div style=""height:24px;""></div>
              <div style=""font-size:12px; color:#64748b;"">If you didn't request this code, you can safely ignore this email.</div>
              <div style=""height:20px;""></div>
              <div style=""font-size:13px; color:#334155;"">Thank you,<br/><span style=""font-weight:700;"">The {AppName} Team</span></div>
            </td>
          </tr>
        </table>
        <div style=""margin-top:20px; font-size:11px; color:#94a3b8;"">© {year} {AppName}. All rights reserved.</div>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    public static string GetVerificationCodePlainText(string verificationCode, string userEmail)
    {
        var year = DateTime.UtcNow.Year.ToString();
        return $@"
VERIFY YOUR EMAIL - {AppName}
========================

Use this code to continue setting up your account for {userEmail}:

  {verificationCode}

This code expires in 15 minutes.

If you didn't request this code, you can safely ignore this email.

Thank you,
The {AppName} Team

© {year} {AppName}. All rights reserved.
";
    }
}
