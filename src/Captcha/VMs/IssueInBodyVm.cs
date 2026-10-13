namespace Captcha.VMs;

/// <summary>
/// Represents a request to issue a new CAPTCHA challenge.
/// </summary>
/// <param name="Purpose">The optional purpose for which the challenge is being issued.</param>
public sealed record IssueInBodyVm(
    string? Purpose
);