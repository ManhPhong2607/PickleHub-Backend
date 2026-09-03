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
            client.BaseAddress = new Uri(configuration["Services:CatalogUrl"]!);
        });

        services.AddHttpClient<IInventoryClient, InventoryHttpClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:InventoryUrl"]!);
        });

        services.AddHttpClient<ICustomerClient, CustomerHttpClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:CustomerUrl"]!);
        });

        services.AddHttpClient<ISystemClient, SystemHttpClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:SystemUrl"]!);
        });

        services.AddHttpClient<IPaymentClient, PaymentHttpClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:PaymentUrl"]!);
        });

        return services;
    }

    public static IServiceCollection AddMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<PaymentCompletedConsumer>();
            x.AddConsumer<PaymentFailedConsumer>();
            x.AddConsumer<StockDepletedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]);
                    h.Password(configuration["RabbitMQ:Password"]);
                });

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
