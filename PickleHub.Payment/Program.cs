using PickleHub.Payment.Application.Common.Interfaces;
using PickleHub.Payment.Extensions;
using PickleHub.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();
