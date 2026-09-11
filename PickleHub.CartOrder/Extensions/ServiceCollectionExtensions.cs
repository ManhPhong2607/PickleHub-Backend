using System.Text;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Domain.Interfaces;
using PickleHub.CartOrder.Infrastructure.Consumers;
using PickleHub.CartOrder.Infrastructure.HttpClients;
using PickleHub.CartOrder.Infrastructure.Persistence;
using PickleHub.Common.Behaviors;

namespace PickleHub.CartOrder.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CartOrderDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CartOrderDb")));

        services.AddScoped<ICartOrderDbContext>(sp => sp.GetRequiredService<CartOrderDbContext>());

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

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<ICatalogClient, CatalogHttpClient>(client =>
        {
            var url = configuration["Services:CatalogUrl"];
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(url) ? "http://127.0.0.1:5002/" : url.TrimEnd('/') + "/");
        });

        services.AddHttpClient<IInventoryClient, InventoryHttpClient>(client =>
        {
            var url = configuration["Services:InventoryUrl"];
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(url) ? "http://127.0.0.1:5005/" : url.TrimEnd('/') + "/");
        });

        services.AddHttpClient<ICustomerClient, CustomerHttpClient>(client =>
        {
            var url = configuration["Services:CustomerUrl"];
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(url) ? "http://127.0.0.1:5003/" : url.TrimEnd('/') + "/");
        });

        services.AddHttpClient<ISystemClient, SystemHttpClient>(client =>
        {
            var url = configuration["Services:SystemUrl"];
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(url) ? "http://127.0.0.1:5004/" : url.TrimEnd('/') + "/");
        });

        services.AddHttpClient<IPaymentClient, PaymentHttpClient>(client =>
        {
            var url = configuration["Services:PaymentUrl"];
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(url) ? "http://127.0.0.1:5008/" : url.TrimEnd('/') + "/");
        });

        return services;
    }

    public static IServiceCollection AddMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitHost = configuration["RabbitMQ:Host"];

        services.AddMassTransit(x =>
        {
            x.AddConsumer<PaymentCompletedConsumer>();
            x.AddConsumer<PaymentFailedConsumer>();
            x.AddConsumer<StockDepletedConsumer>();

            if (!string.IsNullOrWhiteSpace(rabbitHost) && !rabbitHost.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    var vhost = configuration["RabbitMQ:VirtualHost"] ?? "/";
                    if (ushort.TryParse(configuration["RabbitMQ:Port"], out var port) && port > 0)
                    {
                        cfg.Host(rabbitHost, port, vhost, h =>
                        {
                            h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                            h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                        });
                    }
                    else
                    {
                        cfg.Host(rabbitHost, vhost, h =>
                        {
                            h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                            h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                        });
                    }

                    cfg.ReceiveEndpoint("cartorder-payment-completed", e =>
                    {
                        e.ConfigureConsumer<PaymentCompletedConsumer>(ctx);
                    });

                    cfg.ReceiveEndpoint("cartorder-payment-failed", e =>
                    {
                        e.ConfigureConsumer<PaymentFailedConsumer>(ctx);
                    });
                    cfg.ReceiveEndpoint("cartorder-stock-depleted", e =>   
                    {
                        e.ConfigureConsumer<StockDepletedConsumer>(ctx);
                    });
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

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PickleHub CartOrder API",
                Version = "v1"
            });

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
}
