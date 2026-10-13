using System.Threading.RateLimiting;
using BasicLibrary.Cache;
using Captcha.Options;
using Captcha.Services.Generation;
using Captcha.Services.Manage;
using Captcha.Services.Store;
using Microsoft.OpenApi;

namespace Captcha.Extensions;

public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        public async Task AddInternalServices(IConfiguration configuration)
        {
            await services.AddCache(configuration);
            
            services.Configure<CaptchaOptions>(configuration.GetSection(key: "Captcha"));

            // services
            services.AddSingleton<IChallengeStore, RedisChallengeStore>();
            services.AddSingleton<CaptchaImageGenerator>();
            services.AddSingleton<CaptchaManager>();
        }

        public void AddSwagger()
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(name: "v1", new OpenApiInfo
                {
                    Title = "Captcha Service API",
                    Version = "v1",
                    Description = "API for generating and verifying CAPTCHA challenges.",
                    Contact = new OpenApiContact
                    {
                        Name = "CityDi Support",
                        Email = "captcha@citydi.com"
                    }
                });

                // API Key auth
                options.AddSecurityDefinition(name: "ApiKey", new OpenApiSecurityScheme
                {
                    Name = "X-API-Key",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Description = "Enter your API key"
                });
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("ApiKey", document)] = []
                });

                // XML comments for summaries/descriptions
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
            });
        }

        public void AddCors(CaptchaOptions captchaOptions)
        {
            services.AddCors(options => options.AddPolicy(name: "browser", configurePolicy: cors =>
                cors.WithOrigins(captchaOptions.AllowedOrigins).WithMethods("POST").WithHeaders("Content-Type")));
        }

        public void AddLimitRate(CaptchaOptions captchaOptions)
        {
            // Per-instance limiter. With several replicas the effective limit is N x this value;
// use a Redis-backed counter if you need a strict global limit.
            services.AddRateLimiter(opt =>
            {
                opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                opt.OnRejected = (rejected, _) =>
                {
                    if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                        rejected.HttpContext.Response.Headers.RetryAfter = ((int)retry.TotalSeconds).ToString();

                    return ValueTask.CompletedTask;
                };
                opt.AddPolicy("issue", partitioner: http => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = captchaOptions.IssuePerMinutePerIp,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    })
                );
            });
        }
    }
}