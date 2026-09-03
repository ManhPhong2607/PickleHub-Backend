using System.Text;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PickleHub.Notification.Application.Common.Interfaces;
using PickleHub.Notification.Infrastructure.Consumers;
using PickleHub.Notification.Infrastructure.Persistence;
using PickleHub.Notification.Infrastructure.Services;

namespace PickleHub.Notification.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("NotificationDb")
                ?? throw new InvalidOperationException("Thiếu cấu hình ConnectionStrings:NotificationDb.")));

        services.AddScoped<INotificationDbContext>(sp => sp.GetRequiredService<NotificationDbContext>());

        return services;
    }

    public static IServiceCollection AddNotificationServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IRateLimiterService, MemoryCacheRateLimiterService>();
        services.AddHttpClient<IEmailService, ResendEmailService>();
        services.AddSignalR();

        return services;
    }

    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        });

        return services;
    }

    public static IServiceCollection AddMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderCreatedConsumer>();
            x.AddConsumer<PaymentCompletedConsumer>();
            x.AddConsumer<PaymentFailedConsumer>();
            x.AddConsumer<OrderStatusUpdatedConsumer>();
            x.AddConsumer<UserRegisteredConsumer>();
            x.AddConsumer<PasswordResetRequestedConsumer>();
            x.AddConsumer<OrderCancelledConsumer>();
            x.AddConsumer<LowStockAlertConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]);
                    h.Password(configuration["RabbitMQ:Password"]);
                });

                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                cfg.UseInMemoryOutbox();

                cfg.ReceiveEndpoint("notification-order-created", e =>
                {
                    e.ConfigureConsumer<OrderCreatedConsumer>(ctx);
                });

                cfg.ReceiveEndpoint("notification-payment-completed", e =>
                {
                    e.ConfigureConsumer<PaymentCompletedConsumer>(ctx);
                });

                cfg.ReceiveEndpoint("notification-payment-failed", e =>   
                {
                    e.ConfigureConsumer<PaymentFailedConsumer>(ctx);
                });

                cfg.ReceiveEndpoint("notification-order-status-updated", e =>
                {
                    e.ConfigureConsumer<OrderStatusUpdatedConsumer>(ctx);
                });

                cfg.ReceiveEndpoint("notification-user-registered", e =>
                {
                    e.ConfigureConsumer<UserRegisteredConsumer>(ctx);
                });

                cfg.ReceiveEndpoint("notification-password-reset-requested", e =>
                {
                    e.ConfigureConsumer<PasswordResetRequestedConsumer>(ctx);
                });
                cfg.ReceiveEndpoint("notification-order-cancelled", e =>   
                {
                    e.ConfigureConsumer<OrderCancelledConsumer>(ctx);
                });
                cfg.ReceiveEndpoint("notification-low-stock-alert", e =>
                {
                    e.ConfigureConsumer<LowStockAlertConsumer>(ctx);
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
                Title = "PickleHub Notification API",
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
