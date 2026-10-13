namespace Captcha.Services;

public sealed record IssuedChallenge(
    string Id,
    byte[] Png,
    int ExpiresInSeconds,
    int AttemptsAllowed
);