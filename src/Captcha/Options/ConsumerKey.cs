namespace Captcha.Models;

public sealed class ConsumerKey
{
    public string Id { get; set; } = "";
    /// Hex SHA-256 of the consumer's secret key (generate the key from 32 random bytes).
    public string KeySha256Hex { get; set; } = "";
}