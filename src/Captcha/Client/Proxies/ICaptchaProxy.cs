using Captcha.Client.Constants;
using Captcha.VMs;
using Common.Core.Client.Abstractions;
using Common.Core.Client.Attributes;
using Common.Core.Microservice.Domain;
using Refit;

namespace Captcha.Client.Proxies;

/// <summary>
/// 
/// </summary>
public interface ICaptchaProxy : IProxy
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="inBodyVm"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [GetRequest(path: CaptchaUriConstants.Challenge)]
    Task<ApiResult<IssueOutVm>> ChallengeAsync([Body] IssueInBodyVm inBodyVm, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inBodyVm"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [GetRequest(path: CaptchaUriConstants.Verify)]
    Task<ApiResult<VerifyOutVm>> VerifyAsync([Body] VerifyInBodyVm inBodyVm, CancellationToken cancellationToken);
}