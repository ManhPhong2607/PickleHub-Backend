using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Catalog.Infrastructure.Persistence;
using PickleHub.Catalog.Infrastructure.Persistence.Repositories;

using PickleHub.Common.Behaviors;
using PickleHub.Common.Constants;
using PickleHub.Common.Interfaces;
using PickleHub.Common.Service;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

namespace PickleHub.Catalog.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<CatalogDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("CatalogDb")));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());

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
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddSingleton<IStorageService, CloudinaryStorageService>();
            services.AddScoped<IPromotionRepository, PromotionRepository>();
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

        public static IServiceCollection AddMessageBus(this IServiceCollection services, IConfiguration config)
        {
            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(config["RabbitMQ:Host"], "/", h =>
                    {
                        h.Username(config["RabbitMQ:Username"]);
                        h.Password(config["RabbitMQ:Password"]);
                    });

                    cfg.ConfigureEndpoints(ctx);
                });
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

                var internalApiKeyScheme = new OpenApiSecurityScheme
                {
                    Name = AuthConstants.InternalApiKeyHeader,
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "Header nội bộ cho các route /internal/*"
                };

                options.AddSecurityDefinition("Bearer", bearerScheme);
                options.AddSecurityDefinition("InternalApiKey", internalApiKeyScheme);

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

                options.OperationFilter<InternalApiKeyOperationFilter>();
            });

            return services;
        }
    }
}
