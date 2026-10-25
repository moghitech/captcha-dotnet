using System.Security.Cryptography;
using System.Text;
using Captcha.Enums;
using Captcha.Options;
using Captcha.Services.Generation;
using Captcha.Services.Manage.Models;
using Captcha.Services.Store;
using Microsoft.Extensions.Options;

namespace Captcha.Services.Manage;

public sealed class CaptchaManager
{
    // No I/L/O/0/1 and other look-alikes. 24 symbols: 5 chars ~ 22.9 bits, 6 chars ~ 27.5 bits.
    // That is small on purpose (humans have to type it), which is exactly why attempts are capped
    // and /v1/challenges is rate limited.
    private const string Alphabet = "ACDEFGHJKMNPRTUVWXY34679";

    private readonly IChallengeStore _store;
    private readonly CaptchaImageGenerator _images;
    private readonly CaptchaOptions _o;
    private readonly byte[] _macKey;

    public CaptchaManager(IChallengeStore store, CaptchaImageGenerator images, IOptions<CaptchaOptions> options)
    {
        _store = store;
        _images = images;
        _o = options.Value;

        _macKey = Convert.FromBase64String(_o.MacKeyBase64);
        if (_macKey.Length < 32)
            throw new InvalidOperationException("Captcha:MacKeyBase64 must decode to at least 32 bytes.");
    }

    public bool IsKnownPurpose(string? purpose) =>
        purpose is not null && Array.IndexOf(_o.Purposes, purpose) >= 0;

    public async Task<IssuedChallenge> IssueAsync(string purpose)
    {
        var answer = NewAnswer();
        var id = NewId();
#pragma warning disable CS0618 // Type or member is obsolete
        var png = _images.Render(answer); // render first: a failure leaves nothing in Redis
#pragma warning restore CS0618 // Type or member is obsolete

        await _store.PutAsync(id, Mac(id, purpose, answer), purpose, _o.MaxAttempts,
            ttl: TimeSpan.FromSeconds(_o.TtlSeconds));

        return new IssuedChallenge(id, png, _o.TtlSeconds, _o.MaxAttempts);
    }

    public async Task<VerifyOutcome> VerifyAsync(string id, string purpose, string answer)
    {
        // 1. Atomically reserve an attempt (also enforces purpose + expiry + attempt cap).
        var stored = await _store.ConsumeAttemptAsync(id, purpose);
        if (stored is null) return new VerifyOutcome(VerifyStatus.ExpiredOrUsed);

        // 2. Constant-time comparison of MACs.
        var candidate = Mac(id, purpose, normalisedAnswer: Normalize(answer));
        if (!CryptographicOperations.FixedTimeEquals(left: stored.Mac, right: candidate))
            return new VerifyOutcome(VerifyStatus.Incorrect, stored.AttemptsLeft);

        // 3. Single-use gate: among concurrent correct verifications only one DEL returns true.
        return await _store.TryDeleteAsync(id)
            ? new VerifyOutcome(VerifyStatus.Success)
            : new VerifyOutcome(VerifyStatus.ExpiredOrUsed);
    }

    // ---- helpers -------------------------------------------------------------------------

    private byte[] Mac(string id, string purpose, string normalisedAnswer) =>
        // id/purpose/answer are validated to never contain '\n', so the encoding is unambiguous.
        HMACSHA256.HashData(_macKey, source: Encoding.UTF8.GetBytes($"{id}\n{purpose}\n{normalisedAnswer}"));

    private string NewAnswer()
    {
        var chars = new char[_o.AnswerLength];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]; // unbiased CSPRNG
        }

        return new string(chars);
    }

    private static string NewId() =>
        Convert.ToBase64String(inArray: RandomNumberGenerator.GetBytes(count: 16))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_'); // 22 chars

    private static string Normalize(string s) =>
        string.Concat(s.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
}

public static class Validation
{
    public static bool IsId(string? s) =>
        s is { Length: 22 } && s.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    // Anything the user can mistype is a normal "incorrect"; this only bounds the payload size.
    public static bool IsAnswer(string? s) => s is { Length: > 0 and <= 16 };
}