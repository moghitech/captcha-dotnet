namespace Captcha.Options;

/// <summary>
/// Represents a consumer key used to authenticate access to the CAPTCHA service.
/// </summary>
public sealed class ConsumerKey
{
    /// <summary>
    /// Gets or sets the hex-encoded SHA-256 hash of the consumer's secret key. Generate the key from 32 random bytes.
    /// </summary>
    public string KeySha256Hex { get; set; } = "";
}