using PickleHub.Review.Extensions;
using PickleHub.Common.Middleware;

namespace PickleHub.Review;

public static class ReviewApp
{
    public static Task<WebApplication> BuildAsync(string[] args, int port = 5010)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services
            .AddDatabase(builder.Configuration)
            .AddMediator()
            .AddHttpClients(builder.Configuration)
            .AddJwtAuthentication(builder.Configuration)
            .AddSwaggerWithJwt()
            .AddControllers();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PickleHub Review API");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return Task.FromResult(app);
    }
}
