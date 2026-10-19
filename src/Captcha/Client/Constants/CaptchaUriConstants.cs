namespace Captcha.Client.Constants;

/// <summary>
/// Defines URI constants for the CAPTCHA service API endpoints.
/// </summary>
public static class CaptchaUriConstants
{
    private const string Controller = "~/api/v1";

    /// <summary>
    /// The URI for issuing a new CAPTCHA challenge.
    /// </summary>
    public const string Challenge = $"{Controller}/challenges";

    /// <summary>
    /// The URI for verifying a CAPTCHA challenge.
    /// </summary>
    public const string Verify = $"{Controller}/verify";
}