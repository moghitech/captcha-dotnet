namespace Captcha.VMs;

/// <summary>
/// Represents the response returned after verifying a CAPTCHA challenge, including the outcome and optional diagnostic details.
/// </summary>
/// <param name="Success">A value indicating whether the challenge was solved successfully.</param>
/// <param name="Reason">The reason for a failed verification, if applicable.</param>
/// <param name="AttemptsLeft">The number of attempts remaining for the challenge, if applicable.</param>
public sealed record VerifyOutVm(
    bool Success,
    string? Reason = null,
    int? AttemptsLeft = null
);