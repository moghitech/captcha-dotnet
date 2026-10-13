using Captcha.Enums;

namespace Captcha.Services.Manage.Models;

/// <summary>
/// Represents the outcome of verifying a CAPTCHA challenge, including the resulting status and the number of attempts remaining.
/// </summary>
/// <param name="Status">The status resulting from the verification attempt.</param>
/// <param name="AttemptsLeft">The number of attempts remaining for the challenge.</param>
public readonly record struct VerifyOutcome(
    VerifyStatus Status,
    int AttemptsLeft = 0
);