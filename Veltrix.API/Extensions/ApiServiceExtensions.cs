using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Veltrix.API.Filters;
using Veltrix.API.Services;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Infrastructure.Auth;

namespace Veltrix.API.Extensions;

public static class ApiServiceExtensions
{
    public const string AngularCorsPolicy = "Angular";
    public const string AuthRateLimitPolicy = "auth";
    public const string BetsRateLimitPolicy = "bets";
    public const string HubsPath = "/hubs";

    /// <summary>Set to true only when the API is reachable through a reverse proxy alone (production: Caddy).</summary>
    public const string TrustForwardedHeadersKey = "ReverseProxy:TrustForwardedHeaders";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options => options.Filters.Add<FluentValidationFilter>())
            // Enums travel as strings ("Player", "BettingOpen"), which is easier to read in Angular
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Validators that live in the API layer (e.g. AvatarUploadForm, which uses IFormFile)
        services.AddValidatorsFromAssemblyContaining<FluentValidationFilter>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddJwtAuthentication();
        services.AddSwaggerWithJwt();
        services.AddAngularCors(configuration);
        services.AddRateLimiting();
        services.AddRealtime();
        services.AddReverseProxySupport(configuration);

        return services;
    }

    /// <summary>
    /// Behind Caddy every request arrives from Caddy's address over plain HTTP; the real client IP and "https" come in
    /// X-Forwarded-For / X-Forwarded-Proto. Without them the auth rate limit (10 per minute per IP) would be shared by
    /// ALL users, and HTTPS redirection would loop. Trusted only when configured: with no proxy in front, anyone could
    /// fake these headers to dodge the rate limits.
    /// </summary>
    private static void AddReverseProxySupport(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>(TrustForwardedHeadersKey))
            return;

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Docker assigns the proxy's address, so it is not known in advance; the API has no public port,
            // so the only one who can send these headers is the proxy. Only its own (last) entry is used.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = 1;
        });
    }

    private static void AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR()
            // Same enum-as-string format as the REST API
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
        services.AddSingleton<IGameNotifier, SignalRGameNotifier>();
        services.AddSingleton<IOnlineCounter, HubOnlineCounter>();
    }

    private static void AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured lazily from the validated JwtOptions, so a missing key gives a clear startup error
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Keep claim names exactly as written in the token ("sub", "role"), no Microsoft URI mapping
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    // Default is 5 minutes of tolerance; a 15-minute token should really expire after 15 minutes
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtTokenService.NicknameClaim,
                    RoleClaimType = JwtTokenService.RoleClaim
                };

                // Return ProblemDetails for 401/403 too, so the Angular error interceptor sees one format
                options.Events = new JwtBearerEvents
                {
                    // WebSockets cannot send an Authorization header, so the SignalR client
                    // puts the token in the query string. Accept it there for hub URLs only.
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments(HubsPath))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteProblemAsync(context.Response, StatusCodes.Status401Unauthorized, "Unauthorized",
                            "A valid access token is required.");
                    },
                    OnForbidden = context => WriteProblemAsync(context.Response, StatusCodes.Status403Forbidden, "Forbidden",
                        "You do not have permission to perform this action.")
                };
            });

        services.AddAuthorization();
    }

    private static void AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "VELTRIX API",
                Version = "v1",
                Description = "Real-time multiplayer Crash game with VIRTUAL points only (VLX). Educational project."
            });

            options.AddSecurityDefinition(AuthorizeOperationFilter.SchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the accessToken from /api/auth/login (without the 'Bearer ' prefix)."
            });

            // Lock icon only on endpoints with [Authorize] (not a global requirement)
            options.OperationFilter<AuthorizeOperationFilter>();

            // Show the /// XML comments of controllers in Swagger UI
            var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xmlFile))
                options.IncludeXmlComments(xmlFile);
        });
    }

    private static void AddAngularCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };

        services.AddCors(options => options.AddPolicy(AngularCorsPolicy, policy =>
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                // SignalR needs credentials
                .AllowCredentials()));
    }

    private static void AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Max 20 bet/cash-out requests per 10 seconds per user: stops scripted spamming
            options.AddPolicy(BetsRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.User.FindFirst(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub)?.Value
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0
                    }));

            // Max 10 auth requests per minute per IP: slows down password guessing
            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.OnRejected = (context, _) => new ValueTask(WriteProblemAsync(
                context.HttpContext.Response, StatusCodes.Status429TooManyRequests, "Too many requests",
                "Too many requests. Please wait a moment and try again."));
        });
    }

    private static Task WriteProblemAsync(HttpResponse response, int status, string title, string detail)
    {
        response.StatusCode = status;
        return response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = detail, Type = $"https://httpstatuses.io/{status}" },
            (System.Text.Json.JsonSerializerOptions?)null,
            "application/problem+json");
    }
}
