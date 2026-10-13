using Captcha.Enums;

namespace Captcha.Services;

public readonly record struct VerifyOutcome(
    VerifyStatus Status,
    int AttemptsLeft = 0
);