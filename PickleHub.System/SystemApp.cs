using PickleHub.System.Extensions;
using PickleHub.Common.Middleware;

namespace PickleHub.System;

public static class SystemApp
{
    public static async Task<WebApplication> BuildAsync(string[] args, int port = 5004)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services
            .AddDatabase(builder.Configuration)
            .AddRepositories()
            .AddInfrastructureServices()
            .AddMediator()
            .AddJwtAuthentication(builder.Configuration)
            .AddCorsPolicy(builder.Configuration)
            .AddMessageBus(builder.Configuration)
            .AddSwaggerWithJwt()
            .AddAuthorization()
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new global::System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        var app = builder.Build();

        await app.SeedDefaultConfigsAsync();

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

        return app;
    }
}
