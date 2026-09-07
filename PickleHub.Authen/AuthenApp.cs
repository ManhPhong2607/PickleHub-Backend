using PickleHub.Authen.Extensions;
using PickleHub.Common.Middleware;

namespace PickleHub.Authen;

public static class AuthenApp
{
    public static async Task<WebApplication> BuildAsync(string[] args, int port = 5001)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services
            .AddDatabase(builder.Configuration)
            .AddMediator()
            .AddRepositories()
            .AddInfrastructureServices(builder.Configuration)
            .AddJwtAuthentication(builder.Configuration)
            .AddCorsPolicy(builder.Configuration)
            .AddMessageBus(builder.Configuration)
            .AddSwaggerWithJwt()
            .AddControllers();

        var app = builder.Build();

        await app.SeedAdminAsync();

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
