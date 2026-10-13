namespace Captcha.VMs;

/// <summary>
/// Represents the response returned when a new CAPTCHA challenge is issued, including the challenge identifier, rendered image, and validity constraints.
/// </summary>
/// <param name="Id">The unique identifier of the issued challenge.</param>
/// <param name="Image">The Base64-encoded image data of the rendered CAPTCHA challenge.</param>
/// <param name="MimeType">The MIME type of the rendered image.</param>
/// <param name="ExpiresInSeconds">The number of seconds before the challenge expires.</param>
/// <param name="AttemptsAllowed">The number of attempts allowed to solve the challenge.</param>
public sealed record IssueOutVm(
    string Id,
    string Image,
    string MimeType,
    int ExpiresInSeconds,
    int AttemptsAllowed
);