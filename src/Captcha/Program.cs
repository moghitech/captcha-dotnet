using System.Net;
using System.Text.Json.Serialization;
using Captcha.Enums;
using Captcha.Extensions;
using Captcha.Filters;
using Captcha.Options;
using Captcha.Services.Manage;
using Captcha.VMs;
using Common.Core.Microservice.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureServiceOptions();

builder.Services.AddSwagger();
await builder.Services.AddInternalServices(builder.Configuration);

var captchaOptions = builder.Configuration.GetSection(key: "Captcha").Get<CaptchaOptions>() ?? new CaptchaOptions();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

// Real client IP behind nginx: only trust X-Forwarded-For from the proxies you list.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    foreach (var p in captchaOptions.TrustedProxies) o.KnownProxies.Add(IPAddress.Parse(p));
});

builder.Services.AddCors(captchaOptions);
builder.Services.AddLimitRate(captchaOptions);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint(url: "/swagger/v1/swagger.json", name: "Captcha Service API v1");
    c.DocumentTitle = "Captcha Service API";
    c.RoutePrefix = "swagger";
});

app.UseForwardedHeaders();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.CacheControl = "no-store";
    try
    {
        await next();
    }
    catch (RedisException) when (!ctx.Response.HasStarted)
    {
        // Consumers must treat 503 from /v1/verify as a failed check (fail closed).
        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    }
});

app.UseCors();
app.UseRateLimiter();

// Browser-facing: anonymous, CORS allow-list, rate limited per IP.
app.MapPost(pattern: "api/v1/challenges", async (IssueInBodyVm req, CaptchaManager mgr) =>
    {
        if (!mgr.IsKnownPurpose(req.Purpose))
            return Results.Problem(statusCode: 400, title: "Unknown purpose");

        var challenge = await mgr.IssueAsync(req.Purpose!);
        var image = "data:image/png;base64," + Convert.ToBase64String(challenge.Png);

        var response = new IssueOutVm(
            Id: challenge.Id,
            Image: image,
            MimeType: "image/png",
            ExpiresInSeconds: challenge.ExpiresInSeconds,
            AttemptsAllowed: challenge.AttemptsAllowed
        );
        return Results.Json(response, statusCode: StatusCodes.Status201Created);
    })
    .RequireCors("browser")
    .RequireRateLimiting("issue");

// Backend-facing: authenticated per consumer, no CORS (browsers must never call it).
app.MapPost(pattern: "api/v1/verify", async (VerifyInBodyVm req, CaptchaManager mgr) =>
    {
        if (!Validation.IsId(req.Id) || !mgr.IsKnownPurpose(req.Purpose) || !Validation.IsAnswer(req.Answer))
            return Results.Problem(statusCode: 400, title: "Malformed request");

        var r = await mgr.VerifyAsync(req.Id!, req.Purpose!, req.Answer!);
        return Results.Ok(r.Status switch
        {
            VerifyStatus.Success => new VerifyOutVm(Success: true),
            VerifyStatus.Incorrect => new VerifyOutVm(Success: false, Reason: "incorrect", r.AttemptsLeft),
            _ => new VerifyOutVm(Success: false, Reason: "expired_or_used")
        });
    })
    .AddEndpointFilter<ConsumerAuthFilter>();

app.MapGet(pattern: "/healthz", async (IConnectionMultiplexer mux) =>
{
    try
    {
        await mux.GetDatabase().PingAsync();
        return Results.Ok();
    }
    catch (RedisException)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();