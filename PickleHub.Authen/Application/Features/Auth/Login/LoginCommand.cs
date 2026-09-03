using PickleHub.Common.Interfaces;
using FluentValidation;
using MassTransit;
using MassTransit.Transports;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Application.DTOs;
using PickleHub.Authen.Domain.Entities;
using PickleHub.Authen.Domain.Repositories;
using PickleHub.Authen.Infrastructure.Persistence;
using PickleHub.Authen.Infrastructure.Service;
using PickleHub.Common.Events.Authen;
using PickleHub.Common.Exceptions;

namespace PickleHub.Authen.Application.Features.Auth.Login
{
    public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;

    public class LoginHandler : IRequestHandler<LoginCommand, AuthResultDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtTokenService _jwtService;
        private readonly IConfiguration _config;
        private readonly IPublishEndpoint _publishEndpoint;

        public LoginHandler(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork,
            JwtTokenService jwtService,
            IConfiguration config,
            IPublishEndpoint publishEndpoint)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _config = config;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken ct)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, ct);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");

            if (!user.IsEmailVerified)
                throw new ForbiddenException(
                    "Tài khoản chưa được xác minh email. " +
                    "Vui lòng kiểm tra hộp thư và bấm vào link xác minh.");

            if (user.IsBlocked)
                throw new ForbiddenException("Tài khoản đã bị khóa.");

            var refreshDays = int.Parse(_config["Jwt:RefreshTokenDays"]!);
            var refreshTokenValue = _jwtService.GenerateRefreshTokenValue();
            var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshDays);

            _refreshTokenRepository.Add(refreshToken);
            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new UserLoggedInEvent
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                OccurredAt = DateTime.UtcNow
            }, ct);

            return new AuthResultDto
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                AccessToken = _jwtService.GenerateAccessToken(user),
                RefreshToken = refreshTokenValue
            };
        }
    }
}

