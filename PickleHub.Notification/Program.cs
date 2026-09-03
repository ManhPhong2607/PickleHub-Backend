using PickleHub.Notification.Extensions;
using PickleHub.Notification.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();
