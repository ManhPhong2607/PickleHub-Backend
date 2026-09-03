using Microsoft.AspNetCore.Http.Features;
using PickleHub.Catalog.Extensions;
using PickleHub.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Giới hạn tổng dung lượng request - phải đủ lớn để chứa bulk-upload tối đa
// (8 file x 100MB/file theo AddProductImageValidator = tối đa 800MB lý thuyết).
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 850L * 1024 * 1024; // 850 MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 850L * 1024 * 1024; // 850 MB
});

builder.Services
    .AddDatabase(builder.Configuration)
    .AddMediator()
    .AddInfrastructureServices()
    .AddRepositories()
    .AddJwtAuthentication(builder.Configuration)
    .AddCorsPolicy(builder.Configuration)
    .AddSwaggerWithJwt()
    .AddMessageBus(builder.Configuration)
    .AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.Converters.Add(
             new System.Text.Json.Serialization.JsonStringEnumConverter());
     });

builder.Services.AddAuthorization();

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
