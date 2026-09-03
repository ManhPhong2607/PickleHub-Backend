using PickleHub.Common.Interfaces;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Domain.Repositories;
using PickleHub.Authen.Infrastructure.Persistence;
using PickleHub.Common.Events.Authen;
using PickleHub.Common.Exceptions;
using System.Web;

namespace PickleHub.Authen.Application.Features.Auth.ResetPassword
{
    public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

    public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
    {
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        public ResetPasswordHandler(
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint)
        {
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
        {
            // Decode URL-encoded token (in case it's copied from email link with %2F, %2B, %3D etc)
            var decodedToken = HttpUtility.UrlDecode(request.Token);
            var resetToken = await _passwordResetTokenRepository.GetByTokenAsync(decodedToken, ct);

            if (resetToken is null || !resetToken.IsValid)
                throw new UnauthorizedException("Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");

            resetToken.User!.ChangePassword(BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
            resetToken.MarkAsUsed();

            // Thu hồi toàn bộ refresh token — buộc đăng nhập lại
            var activeTokens = await _refreshTokenRepository
                .GetActiveByUserIdAsync(resetToken.UserId, ct);

            foreach (var t in activeTokens)
                t.Revoke();

            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new UserPasswordChangedEvent
            {
                UserId = resetToken.User!.Id,
                Email = resetToken.User!.Email,
                Action = "Reset",
                OccurredAt = DateTime.UtcNow
            }, ct);
        }
    }
}
