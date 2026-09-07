using Microsoft.AspNetCore.Http.Features;
using PickleHub.Catalog.Extensions;
using PickleHub.Common.Middleware;

namespace PickleHub.Catalog;

public static class CatalogApp
{
    public static Task<WebApplication> BuildAsync(string[] args, int port = 5002)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 104_857_600;
        });

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 104_857_600;
        });

        builder.Services
            .AddDatabase(builder.Configuration)
            .AddMediator()
            .AddInfrastructureServices()
            .AddRepositories()
            .AddJwtAuthentication(builder.Configuration)
            .AddCorsPolicy(builder.Configuration)
            .AddSwaggerWithJwt()
            .AddMessageBus(builder.Configuration)
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        builder.Services.AddAuthorization();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return Task.FromResult(app);
    }
}
