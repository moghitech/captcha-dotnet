namespace Captcha.VMs;

/// <summary>
/// Represents a request to verify a CAPTCHA challenge.
/// </summary>
/// <param name="Id">The unique identifier of the challenge to verify.</param>
/// <param name="Purpose">The purpose for which the challenge was issued.</param>
/// <param name="Answer">The solution provided by the user for the challenge.</param>
public sealed record VerifyInBodyVm(
    string? Id,
    string? Purpose,
    string? Answer
);