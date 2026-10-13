namespace Captcha.Store.Models;

public sealed record ConsumedChallenge(
    byte[] Mac,
    int AttemptsLeft
);