using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PickleHub.Common.Behaviors;
using PickleHub.Common.Interfaces;
using PickleHub.Common.Service;
using PickleHub.Customers.Domain.Repositories;
using PickleHub.Customers.Infrastructure.Persistence.Repositories;
using PickleHub.Customers.Infrastructure.Persistence;
using System.Text;

namespace PickleHub.Customers.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(
            this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<CustomerDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("CustomerDb")));
            services.AddScoped<IUnitOfWork>(sp =>
                sp.GetRequiredService<CustomerDbContext>());
            return services;
        }

        public static IServiceCollection AddRepositories(
            this IServiceCollection services)
        {
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
            services.AddScoped<ILoyaltyTierRepository, LoyaltyTierRepository>();
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

        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
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
            var origins = config.GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod());
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
                    new KebabCaseEndpointNameFormatter("customers", false));

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
    }
}
