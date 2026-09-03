using PickleHub.Inventory.Extensions;
using PickleHub.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDatabase(builder.Configuration)
    .AddRepositories()
    .AddMediator()
    .AddApplicationOptions(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddHttpClients(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddCorsPolicy(builder.Configuration)
    .AddMessageBus(builder.Configuration)
    .AddSwaggerWithJwt()
    .AddAuthorization(options =>
    {
        options.AddPolicy("ServiceClientPolicy", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") ||
                context.User.IsInRole("ServiceClient") ||
                context.User.HasClaim(c => (c.Type == "scope" || c.Type == "client_id" || c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role) &&
                                           (c.Value.Contains("internal_service") || c.Value.Contains("ServiceClient") || c.Value.Contains("Admin"))));
        });
    })
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
