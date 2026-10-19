using Captcha.Client.Handlers;
using Captcha.Client.Proxies;
using Common.Core.Client;
using Common.Core.Client.Extensions;

namespace Captcha.Client.Extensions;

/// <summary>
/// Provides extension methods for registering the CAPTCHA client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the CAPTCHA service proxy and its consumer authentication handler with the dependency injection container using the configured service URL.
    /// </summary>
    /// <param name="builder">The web application builder whose services are configured.</param>
    /// <param name="version">The API version used when constructing the client URI.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated service collection.</returns>
    public static async Task<IServiceCollection> AddCaptcha(this WebApplicationBuilder builder, string version = "v1")
    {
        var url = builder.Configuration["ServiceOptions:CaptchaOptions:Url"];

        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Invalid url for captcha service to retrieve.");

        var clientUri = new ClientUri(BaseUrl: url, version);
        builder.Services.SetupAuthenticatedProxy<ICaptchaProxy, CaptchaConsumerAuthHandler>(clientUri);

        return builder.Services;
    }
}