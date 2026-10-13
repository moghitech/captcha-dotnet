namespace Captcha.Services.Manage.Models;

/// <summary>
/// Represents an issued CAPTCHA challenge, including its identifier, rendered image, and validity constraints.
/// </summary>
/// <param name="Id">The unique identifier of the issued challenge.</param>
/// <param name="Png">The PNG image data of the rendered CAPTCHA challenge.</param>
/// <param name="ExpiresInSeconds">The number of seconds before the challenge expires.</param>
/// <param name="AttemptsAllowed">The number of attempts allowed to solve the challenge.</param>
public sealed record IssuedChallenge(
    string Id,
    byte[] Png,
    int ExpiresInSeconds,
    int AttemptsAllowed
);