using Captcha.Client.Constants;
using Captcha.VMs;
using Common.Core.Client.Abstractions;
using Common.Core.Client.Attributes;
using Common.Core.Microservice.Domain;
using Refit;

namespace Captcha.Client.Proxies;

/// <summary>
/// Defines the proxy for communicating with the CAPTCHA service API.
/// </summary>
public interface ICaptchaProxy : IProxy
{
    /// <summary>
    /// Requests a new CAPTCHA challenge from the CAPTCHA service.
    /// </summary>
    /// <param name="inBodyVm">The request body containing the challenge issue parameters.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the issued challenge details.</returns>
    [GetRequest(path: CaptchaUriConstants.Challenge)]
    Task<ApiResult<IssueOutVm>> ChallengeAsync([Body] IssueInBodyVm inBodyVm, CancellationToken cancellationToken);

    /// <summary>
    /// Submits a solution to the CAPTCHA service for verification.
    /// </summary>
    /// <param name="inBodyVm">The request body containing the challenge verification parameters.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the verification outcome.</returns>
    [GetRequest(path: CaptchaUriConstants.Verify)]
    Task<ApiResult<VerifyOutVm>> VerifyAsync([Body] VerifyInBodyVm inBodyVm, CancellationToken cancellationToken);
}