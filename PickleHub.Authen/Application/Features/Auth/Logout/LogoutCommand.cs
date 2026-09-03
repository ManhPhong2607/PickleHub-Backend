using PickleHub.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Domain.Repositories;
using PickleHub.Authen.Infrastructure.Persistence;
using PickleHub.Common.Exceptions;

namespace PickleHub.Authen.Application.Features.Auth.Logout
{
    public record LogoutCommand(string RefreshToken) : IRequest;

    public class LogoutHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LogoutHandler(IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(LogoutCommand request, CancellationToken ct)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);

            if (token == null || !token.IsActive)
                throw new UnauthorizedException("Refresh token không hợp lệ.");

            token.Revoke();
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
