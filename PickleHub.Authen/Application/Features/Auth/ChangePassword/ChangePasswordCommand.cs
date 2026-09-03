using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Domain.Repositories;
using PickleHub.Authen.Infrastructure.Persistence;
using PickleHub.Common.Events.Authen;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Authen.Application.Features.Auth.ChangePassword
{
    public record ChangePasswordCommand(
     string OldPassword,
     string NewPassword) : IRequest;

    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPublishEndpoint _publishEndpoint;
        public ChangePasswordHandler(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IPublishEndpoint publishEndpoint)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
        {
            if (!_currentUserService.IsAuthenticated)
                throw new UnauthorizedException("Không xác định được người dùng.");

            var user = await _userRepository.GetByIdAsync(_currentUserService.UserId, ct)
                ?? throw new NotFoundException("Không tìm thấy người dùng.");

            if (user.IsBlocked)
                throw new ForbiddenException("Tài khoản đã bị khóa.");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                throw new DomainException("Mật khẩu hiện tại không đúng.");

            user.ChangePassword(BCrypt.Net.BCrypt.HashPassword(request.NewPassword));

            var activeTokens = await _refreshTokenRepository
                .GetActiveByUserIdAsync(user.Id, ct);

            foreach (var t in activeTokens)
                t.Revoke();

            await _unitOfWork.SaveChangesAsync(ct);
            await _publishEndpoint.Publish(new UserPasswordChangedEvent
            {
                UserId = user.Id,
                Email = user.Email,
                Action = "Changed",
                OccurredAt = DateTime.UtcNow
            }, ct);
        }
    }
}