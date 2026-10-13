namespace Captcha.Models;

public sealed class CaptchaOptions
{
    /// 32+ random bytes, base64. Load from env / secret store, never from a committed file.
    public string MacKeyBase64 { get; set; } = "";
 
    public int AnswerLength { get; set; } = 5;
    public int TtlSeconds { get; set; } = 120;
    public int MaxAttempts { get; set; } = 3;
 
    public int Width { get; set; } = 200;
    public int Height { get; set; } = 70;
    public string FontsDirectory { get; set; } = "fonts";
 
    public string[] Purposes { get; set; } = [];
    public int IssuePerMinutePerIp { get; set; } = 20;
    public string[] AllowedOrigins { get; set; } = [];
    public string[] TrustedProxies { get; set; } = [];
    public ConsumerKey[] Consumers { get; set; } = [];
}