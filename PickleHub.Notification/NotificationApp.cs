using PickleHub.Notification.Extensions;
using PickleHub.Notification.Infrastructure.Hubs;

namespace PickleHub.Notification;

public static class NotificationApp
{
    public static Task<WebApplication> BuildAsync(string[] args, int port = 5009)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services
            .AddDatabase(builder.Configuration)
            .AddNotificationServices()
            .AddMediator()
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PickleHub Notification API");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<NotificationHub>("/hubs/notifications");

        return Task.FromResult(app);
    }
}
