using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PickleHub.Authen.Domain.Repositories;
using PickleHub.Authen.Infrastructure.Persistence;
using PickleHub.Authen.Infrastructure.Persistence.Repositories;
using PickleHub.Authen.Infrastructure.Service;
using PickleHub.Common.Behaviors;
using PickleHub.Common.Interfaces;
using PickleHub.Common.Service;
using System.Text;

namespace PickleHub.Authen.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AuthenDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("AuthenDb")));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AuthenDbContext>());

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

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
            services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<JwtTokenService>();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

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

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            var secretKey = config["Jwt:SecretKey"]!;
            var issuer = config["Jwt:Issuer"]!;

            services.AddAuthorization();
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
                    
                    // Debug: Show JWT errors
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"JWT Authentication Failed: {context.Exception.Message}");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine("JWT Token Validated Successfully");
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
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

        public static IServiceCollection AddMessageBus(
          this IServiceCollection services, IConfiguration config)
        {
            services.AddMassTransit(x =>
            {
                x.AddConsumers(typeof(Program).Assembly);

                x.SetEndpointNameFormatter(
                    new KebabCaseEndpointNameFormatter("authen", false));

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
    }
}

