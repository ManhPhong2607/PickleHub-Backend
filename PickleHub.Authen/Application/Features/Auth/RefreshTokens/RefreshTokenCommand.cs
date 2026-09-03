using PickleHub.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Application.DTOs;
using PickleHub.Authen.Domain.Entities;
using PickleHub.Authen.Infrastructure.Service;
using PickleHub.Authen.Infrastructure.Persistence.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Authen.Domain.Repositories;

namespace PickleHub.Authen.Application.Features.Auth.RefreshTokens
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtTokenService _jwtService;
        private readonly IConfiguration _config;

        public RefreshTokenHandler(
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork,
            JwtTokenService jwtService,
            IConfiguration config)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _config = config;
        }

        public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken ct)
        {
            var existing = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);

            if (existing is null || !existing.IsActive)
                throw new UnauthorizedException("Refresh token không hợp lệ hoặc đã hết hạn.");

            var user = existing.User
                ?? throw new UnauthorizedException("Không tìm thấy người dùng.");

            if (user.IsBlocked)
                throw new ForbiddenException("Tài khoản đã bị khóa.");

            existing.Revoke();

            var refreshDays = int.Parse(_config["Jwt:RefreshTokenDays"]!);
            var newTokenValue = _jwtService.GenerateRefreshTokenValue();
            var newToken = RefreshToken.Create(existing.UserId, newTokenValue, refreshDays);

            _refreshTokenRepository.Add(newToken);
            await _unitOfWork.SaveChangesAsync(ct);

            return new AuthResultDto
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                AccessToken = _jwtService.GenerateAccessToken(user),
                RefreshToken = newTokenValue
            };
        }
    }
}

