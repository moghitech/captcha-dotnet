namespace Captcha.Client.Handlers;

/// <summary>
/// A delegating handler that attaches the configured consumer key to outgoing CAPTCHA service requests via the <c>X-Consumer-Key</c> header.
/// </summary>
/// <param name="configuration">The configuration source used to retrieve the consumer key.</param>
public sealed class CaptchaConsumerAuthHandler(
    IConfiguration configuration
) : DelegatingHandler
{
    /// <summary>
    /// Sends the HTTP request after adding the consumer key header required for authenticating with the CAPTCHA service.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = configuration["ServiceOptions:CaptchaOptions:ConsumerKey"];

        request.Headers.Add(name: "X-Consumer-Key", key);

        return await base.SendAsync(request, cancellationToken);
    }
}