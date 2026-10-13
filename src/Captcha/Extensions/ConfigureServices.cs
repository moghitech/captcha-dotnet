using Microsoft.OpenApi;

namespace Captcha.Extensions;

public static class ConfigureSwagger
{
    public static void AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Captcha Service API",
                Version = "v1",
                Description = "API for generating and verifying CAPTCHA challenges.",
                Contact = new OpenApiContact
                {
                    Name = "City DI Support",
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

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // XML comments for summaries/descriptions
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
        });
    }
}