using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PickleHub.Common.Behaviors;
using PickleHub.Common.Interfaces;
using PickleHub.Common.Service;
using PickleHub.Inventory.Application.Settings;
using PickleHub.Inventory.Domain.Repositories;
using PickleHub.Inventory.Infrastructure.Persistence.Repositories;
using PickleHub.Inventory.Infrastructure.Persistence;
using System.Text;
using PickleHub.Inventory.Application.Common.Interfaces;
using PickleHub.Inventory.Infrastructure.HttpClients;
using PickleHub.Inventory.Application.Common;

namespace PickleHub.Inventory.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(
            this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<InventoryDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("InventoryDb")));
            services.AddScoped<IUnitOfWork>(sp =>
                sp.GetRequiredService<InventoryDbContext>());
            return services;
        }

        public static IServiceCollection AddRepositories(
            this IServiceCollection services)
        {
            services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
            services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();

            return services;
        }

        public static IServiceCollection AddMediator(
            this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(typeof(Program).Assembly);
            return services;
        }

        public static IServiceCollection AddApplicationOptions(
            this IServiceCollection services, IConfiguration config)
        {
            services.Configure<InventorySettings>(
                config.GetSection(InventorySettings.SectionName));
            return services;
        }
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<StockOperationExecutor>();
            return services;
        }

        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, IConfiguration config)
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
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(secretKey))
                    };
                });

            return services;
        }

        public static IServiceCollection AddCorsPolicy(
            this IServiceCollection services, IConfiguration config)
        {
            var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    if (origins != null && origins.Length > 0)
                    {
                        policy.WithOrigins(origins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
                    else
                    {
                        policy.SetIsOriginAllowed(_ => true)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
                });
            });

            return services;
        }

        public static IServiceCollection AddMessageBus(
            this IServiceCollection services, IConfiguration config)
        {
            services.AddMassTransit(x =>
            {
                x.AddConsumers(typeof(Program).Assembly);

                x.SetEndpointNameFormatter(
                    new KebabCaseEndpointNameFormatter("inventory", false));

                var rabbitHost = config["RabbitMQ:Host"];

                if (!string.IsNullOrWhiteSpace(rabbitHost) && !rabbitHost.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    x.UsingRabbitMq((ctx, cfg) =>
                    {
                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                        var vhost = config["RabbitMQ:VirtualHost"] ?? "/";
                        if (ushort.TryParse(config["RabbitMQ:Port"], out var port) && port > 0)
                        {
                            cfg.Host(rabbitHost, port, vhost, h =>
                            {
                                h.Username(config["RabbitMQ:Username"] ?? "guest");
                                h.Password(config["RabbitMQ:Password"] ?? "guest");
                            });
                        }
                        else
                        {
                            cfg.Host(rabbitHost, vhost, h =>
                            {
                                h.Username(config["RabbitMQ:Username"] ?? "guest");
                                h.Password(config["RabbitMQ:Password"] ?? "guest");
                            });
                        }

                        cfg.ConfigureEndpoints(ctx);
                    });
                }
                else
                {
                    x.UsingInMemory((ctx, cfg) =>
                    {
                        cfg.ConfigureEndpoints(ctx);
                    });
                }
            });

            return services;
        }

        public static IServiceCollection AddSwaggerWithJwt(
            this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                var scheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập JWT token. Ví dụ: eyJhbGci..."
                };

                options.AddSecurityDefinition("Bearer", scheme);
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

        public static IServiceCollection AddHttpClients(
          this IServiceCollection services, IConfiguration config)
        {
            var catalogUrl = config["Services:CatalogUrl"];
            if (string.IsNullOrWhiteSpace(catalogUrl))
            {
                catalogUrl = "http://127.0.0.1:5002";
            }

            services.AddHttpClient<ICatalogClient, CatalogHttpClient>(client =>
            {
                client.BaseAddress = new Uri(catalogUrl.TrimEnd('/') + "/");
            });
            return services;
        }
    }
}
