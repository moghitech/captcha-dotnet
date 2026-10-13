namespace Captcha.Options;

/// <summary>
/// Represents the configuration settings for the CAPTCHA service, including cryptographic keys, challenge parameters, rendering options, and access control settings.
/// </summary>
public sealed class CaptchaOptions
{
    /// <summary>
    /// Gets or sets the Base64-encoded MAC key (32+ random bytes) used for signing challenges. Load from environment variables or a secret store, never from a committed file.
    /// </summary>
    public string MacKeyBase64 { get; set; } = "";

    /// <summary>
    /// Gets or sets the number of characters in the generated CAPTCHA answer.
    /// </summary>
    public int AnswerLength { get; set; } = 5;

    /// <summary>
    /// Gets or sets the time-to-live of a challenge in seconds.
    /// </summary>
    public int TtlSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets the maximum number of attempts allowed to solve a challenge.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the width of the rendered CAPTCHA image in pixels.
    /// </summary>
    public int Width { get; set; } = 200;

    /// <summary>
    /// Gets or sets the height of the rendered CAPTCHA image in pixels.
    /// </summary>
    public int Height { get; set; } = 70;

    /// <summary>
    /// Gets or sets the directory containing the fonts used to render the CAPTCHA image.
    /// </summary>
    public string FontsDirectory { get; set; } = "fonts";

    /// <summary>
    /// Gets or sets the list of allowed purposes for which challenges may be issued.
    /// </summary>
    public string[] Purposes { get; set; } = [];

    /// <summary>
    /// Gets or sets the maximum number of challenges that may be issued per minute per IP address.
    /// </summary>
    public int IssuePerMinutePerIp { get; set; } = 20;

    /// <summary>
    /// Gets or sets the list of origins allowed to access the CAPTCHA service.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of trusted proxy addresses used when resolving client IP addresses.
    /// </summary>
    public string[] TrustedProxies { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of consumer keys authorized to access the CAPTCHA service.
    /// </summary>
    public ConsumerKey[] Consumers { get; set; } = [];
}