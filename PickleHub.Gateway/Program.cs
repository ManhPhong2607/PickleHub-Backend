// Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PickleHub.Gateway.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    jwtSecret = "8PFVpQHemzQ2RDJpDcSM5BIlTIhoM6LgvZFudvwRhVY=";
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PickleHub.Authen";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    "{\"error\":{\"message\":\"Bạn cần đăng nhập để thực hiện thao tác này.\"}}");
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    "{\"error\":{\"message\":\"Bạn không có quyền thực hiện thao tác này.\"}}");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("admin-only", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin"));
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var rawOrigins = builder.Configuration["Cors:AllowedOrigins"];
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? rawOrigins?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (origins != null && origins.Length > 0 && !origins.Contains("*"))
        {
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// 1. Log real-time requests to stdout for Render Logs
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var path = context.Request.Path;
    var query = context.Request.QueryString;
    var origin = context.Request.Headers["Origin"].ToString();
    Console.WriteLine($"[GATEWAY IN] {method} {path}{query} | Origin: {origin}");

    await next();

    Console.WriteLine($"[GATEWAY OUT] {method} {path} => {context.Response.StatusCode}");
});

// 2. Short-circuit Preflight (OPTIONS) requests with CORS headers
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].ToString();
    if (!string.IsNullOrEmpty(origin))
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
        context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With, X-Session-Id, Accept, Origin";
    }

    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.Headers["Access-Control-Max-Age"] = "86400";
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    await next();
});

app.UseCors();

// 3. Health check for Render Web Service health checks & diagnostics
app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow, version = "1.0.2" }));

app.UseAuthentication();

// Forward claims sau khi authenticate
app.UseMiddleware<JwtForwardingMiddleware>();

app.UseAuthorization();

// YARP handle routing
app.MapReverseProxy();

app.Run();
