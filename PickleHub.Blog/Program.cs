using Microsoft.AspNetCore.Http.Features;
using PickleHub.Blog.Extensions;
using PickleHub.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Tăng giới hạn upload file lên 100MB 
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104_857_600; // 100 MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104_857_600; // 100 MB
});

builder.Services
    .AddDatabase(builder.Configuration)
    .AddMediator()
    .AddInfrastructureServices()
    .AddRepositories()
    .AddCatalogClient(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddCorsPolicy(builder.Configuration)
    .AddSwaggerWithJwt()
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
