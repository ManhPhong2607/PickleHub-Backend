using PickleHub.AuditLog.Extensions;
using PickleHub.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();
