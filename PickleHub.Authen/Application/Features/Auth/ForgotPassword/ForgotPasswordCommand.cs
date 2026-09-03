using PickleHub.Common.Interfaces;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Domain.Entities;
using PickleHub.Authen.Domain.Repositories;
using PickleHub.Authen.Infrastructure.Persistence;
using PickleHub.Authen.Infrastructure.Service;
using PickleHub.Common.Events.Authen;

namespace PickleHub.Authen.Application.Features.Auth.ForgotPassword
{
    public record ForgotPasswordCommand(string Email) : IRequest;
    public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtTokenService _jwtService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IConfiguration _config;

        public ForgotPasswordHandler(
            IUserRepository userRepository,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IUnitOfWork unitOfWork,
            JwtTokenService jwtService,
            IPublishEndpoint publishEndpoint,
            IConfiguration config)
        {
            _userRepository = userRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _publishEndpoint = publishEndpoint;
            _config = config;
        }

        public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, ct);

            if (user == null || user.IsBlocked) return;

            // Vô hiệu hóa các token cũ chưa dùng
            var oldTokens = await _passwordResetTokenRepository
                .GetActiveByUserIdAsync(user.Id, ct);

            foreach (var old in oldTokens)
                old.MarkAsUsed();

            var tokenValue = _jwtService.GeneratePasswordResetToken();
            var resetToken = PasswordResetToken.Create(user.Id, tokenValue);

            _passwordResetTokenRepository.Add(resetToken);
            await _unitOfWork.SaveChangesAsync(ct);

            var resetLink = $"{_config["App:BaseUrl"]}/reset-password?token={tokenValue}";
            await _publishEndpoint.Publish(new PasswordResetRequestedEvent
            {
                UserId = user.Id,
                Email = user.Email,
                CustomerName = user.Email.Split('@')[0],
                ResetToken = tokenValue,
                ResetUrl = resetLink,
                RequestedAt = DateTime.UtcNow
            }, ct);
        }
    }
}

