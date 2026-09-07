using PickleHub.CartOrder.Extensions;
using PickleHub.Common.Middleware;

namespace PickleHub.CartOrder;

public static class CartOrderApp
{
    public static Task<WebApplication> BuildAsync(string[] args, int port = 5007)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services
            .AddDatabase(builder.Configuration)
            .AddMediator()
            .AddHttpClients(builder.Configuration)
            .AddMessageBus(builder.Configuration)
            .AddJwtAuthentication(builder.Configuration)
            .AddSwaggerWithJwt()
            .AddControllers();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PickleHub CartOrder API");
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
