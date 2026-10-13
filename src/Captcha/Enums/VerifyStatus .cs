namespace Captcha.Enums;

/// <summary>
/// Specifies the result status of a CAPTCHA challenge verification attempt.
/// </summary>
public enum VerifyStatus
{
    /// <summary>
    /// Indicates that the challenge was solved correctly.
    /// </summary>
    Success,

    /// <summary>
    /// Indicates that the provided solution was incorrect.
    /// </summary>
    Incorrect,

    /// <summary>
    /// Indicates that the challenge has expired or has already been used.
    /// </summary>
    ExpiredOrUsed
}