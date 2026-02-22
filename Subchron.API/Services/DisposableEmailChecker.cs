namespace Subchron.API.Services;

// Detects disposable/temp email domains so signup can reject them (e.g. yopmail).
public static class DisposableEmailChecker
{
    private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "yopmail.com", "yopmail.fr", "cool.fr", "jetable.fr", "nospam.ze.tl", "nomail.xl.cx",
        "mega.zik.dj", "speed.1s.fr", "courriel.fr.nf", "moncourrier.fr.nf", "monemail.fr.nf", "monmail.fr.nf",
        "hide.biz.st", "emailtemporanea.com", "guerrillamail.com", "guerrillamail.org", "guerrillamail.net",
        "guerrillamail.biz", "guerrillamail.de", "grr.la", "guerrillamail.info", "sharklasers.com",
        "spam4.me", "10minutemail.com", "10minutemail.net", "tempmail.com", "temp-mail.org",
        "throwaway.email", "mailinator.com", "mailinator.net", "mailinator2.com", "trashmail.com",
        "getnada.com", "fakeinbox.com", "tmpeml.com", "dispostable.com", "maildrop.cc",
        "tempail.com", "mohmal.com", "emailondeck.com", "mintemail.com", "33mail.com"
    };

    public static bool IsDisposable(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var at = email.IndexOf('@');
        if (at <= 0 || at == email.Length - 1) return false;
        var domain = email[(at + 1)..].Trim().ToLowerInvariant();
        return DisposableDomains.Contains(domain);
    }
}
