namespace Captcha.Services.Store.Models;

/// <summary>
/// Represents a consumed CAPTCHA challenge, including its message authentication code and the number of attempts remaining.
/// </summary>
/// <param name="Mac">The message authentication code associated with the consumed challenge.</param>
/// <param name="AttemptsLeft">The number of attempts remaining for the challenge.</param>
public sealed record ConsumedChallenge(
    byte[] Mac,
    int AttemptsLeft
);