using System.Collections.Concurrent;
using Subchron.API.Models.Auth;

namespace Subchron.API.Services;

// Keeps signup drafts in memory until billing is done; the real account is created only after that.
public static class SignupDraftStore
{
    private static readonly ConcurrentDictionary<string, (SignupDraftData Data, DateTime ExpiresAt)> Store = new();

    private static readonly TimeSpan DraftExpiry = TimeSpan.FromMinutes(30);

    public static string Save(SignupDraftData data)
    {
        var token = Guid.NewGuid().ToString("N");
        Store[token] = (data, DateTime.UtcNow.Add(DraftExpiry));
        return token;
    }

    public static SignupDraftData? GetAndRemove(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (!Store.TryRemove(token, out var entry)) return null;
        if (entry.ExpiresAt < DateTime.UtcNow) return null;
        return entry.Data;
    }

    public static SignupDraftData? Get(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (!Store.TryGetValue(token, out var entry)) return null;
        if (entry.ExpiresAt < DateTime.UtcNow) return null;
        return entry.Data;
    }
}
