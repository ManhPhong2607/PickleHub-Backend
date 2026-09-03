using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PickleHub.Blog.Application.Common.Interfaces;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Blog.Infrastructure.Persistence.Repositories;
using PickleHub.Blog.Infrastructure.Service;
using PickleHub.Common.Behaviors;
using PickleHub.Common.Constants;
using PickleHub.Common.Interfaces;
using PickleHub.Common.Service;
using PickleHub.Blog.Infrastructure.Persistence;
using System.Text;

namespace PickleHub.Blog.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<BlogDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("BlogDb")));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BlogDbContext>());

            return services;
        }

        public static IServiceCollection AddMediator(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(typeof(Program).Assembly);

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<IContentCategoryRepository, ContentCategoryRepository>();
            services.AddSingleton<IStorageService, CloudinaryStorageService>();

            return services;
        }

        public static IServiceCollection AddCatalogClient(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpClient<ICatalogClient, CatalogHttpClient>(client =>
            {
                client.BaseAddress = new Uri(config["Services:CatalogUrl"]!);
                client.DefaultRequestHeaders.Add(AuthConstants.InternalApiKeyHeader, config["Security:InternalApiKey"]);
            });

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            var secretKey = config["Jwt:SecretKey"]!;
            var issuer = config["Jwt:Issuer"]!;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = issuer,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                    };
                });

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
        {
            var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod());
            });

            return services;
        }

        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                var bearerScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập JWT token. Ví dụ: eyJhbGci..."
                };

                options.AddSecurityDefinition("Bearer", bearerScheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        []
                    }
                });
            });
            return services;
        }
    }
}
