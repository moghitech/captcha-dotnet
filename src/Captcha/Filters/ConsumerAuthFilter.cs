using System.Security.Cryptography;
using System.Text;
using Captcha.Options;
using Microsoft.Extensions.Options;

namespace Captcha.Filters;

/// <summary>
/// An endpoint filter that authenticates incoming requests by validating the consumer key presented in the <c>X-Consumer-Key</c>
/// header against the configured set of consumer key hashes using a constant-time comparison.
/// </summary>
public sealed class ConsumerAuthFilter : IEndpointFilter
{
    private readonly byte[][] _keyHashes;

    public ConsumerAuthFilter(IOptionsSnapshot<CaptchaOptions> options) =>
        _keyHashes = options.Value.Consumers.Select(consumerKey => Convert.FromHexString(consumerKey.KeySha256Hex)).ToArray();

    /// <summary>
    /// Validates the consumer key presented in the request header and either invokes the next filter or returns an unauthorized result.
    /// </summary>
    /// <param name="ctx">The endpoint filter invocation context.</param>
    /// <param name="next">The next filter delegate in the pipeline.</param>
    /// <returns>The result of the next filter if authentication succeeds; otherwise, an unauthorized result.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var presented = ctx.HttpContext.Request.Headers["X-Consumer-Key"].ToString();
        if (presented.Length is 0 or > 256) return Results.Unauthorized();

        var hash = SHA256.HashData(source: Encoding.UTF8.GetBytes(presented));

        var ok = _keyHashes.Aggregate(false, (current, k)
            => current | CryptographicOperations.FixedTimeEquals(left: hash, right: k));

        return ok ? await next(ctx) : Results.Unauthorized();
    }
}