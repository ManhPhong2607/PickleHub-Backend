using PickleHub.Customers.Extensions;
using PickleHub.Common.Middleware;

namespace PickleHub.Customers;

public static class CustomersApp
{
    public static Task<WebApplication> BuildAsync(string[] args, int port = 5003)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services
            .AddDatabase(builder.Configuration)
            .AddRepositories()
            .AddMediator()
            .AddInfrastructureServices()
            .AddJwtAuthentication(builder.Configuration)
            .AddCorsPolicy(builder.Configuration)
            .AddMessageBus(builder.Configuration)
            .AddSwaggerWithJwt()
            .AddAuthorization()
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

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
