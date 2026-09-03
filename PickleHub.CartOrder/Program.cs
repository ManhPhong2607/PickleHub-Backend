using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Extensions;
using PickleHub.CartOrder.Infrastructure.Persistence;
using PickleHub.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDatabase(builder.Configuration)
    .AddMediator()
    .AddHttpClients(builder.Configuration)
    .AddMessageBus(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddSwaggerWithJwt()
    .AddControllers();

var app = builder.Build();

// Auto-migrate & Seed CartOrder Data
// using (var scope = app.Services.CreateScope())
// {
//     try
//     {
//         var dbContext = scope.ServiceProvider.GetRequiredService<CartOrderDbContext>();
//         // await CartOrderDataSeeder.SeedAsync(dbContext);
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"[CartOrderDataSeeder Error] {ex.Message}");
//     }
// }

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

app.Run();
