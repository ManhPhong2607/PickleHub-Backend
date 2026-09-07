using Microsoft.AspNetCore.Http.Features;
using PickleHub.Blog.Extensions;
using PickleHub.Blog.Infrastructure.Persistence;
using PickleHub.Common.Middleware;

namespace PickleHub.Blog;

public static class BlogApp
{
    public static async Task<WebApplication> BuildAsync(string[] args, int port = 5011)
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
            .AddCatalogClient(builder.Configuration)
            .AddJwtAuthentication(builder.Configuration)
            .AddCorsPolicy(builder.Configuration)
            .AddSwaggerWithJwt()
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

        await app.MigrateAndSeedBlogAsync();

        return app;
    }
}
