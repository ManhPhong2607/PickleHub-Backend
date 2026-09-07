using PickleHub.Payment.Extensions;
using PickleHub.Common.Middleware;

namespace PickleHub.Payment;

public static class PaymentApp
{
    public static Task<WebApplication> BuildAsync(string[] args, int port = 5008)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services
            .AddDatabase(builder.Configuration)
            .AddPayOS(builder.Configuration)
            .AddHttpClients(builder.Configuration)
            .AddMessageBus(builder.Configuration)
            .AddMediator()
            .AddJwtAuthentication(builder.Configuration)
            .AddSwaggerWithJwt()
            .AddControllers();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PickleHub Payment API");
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
