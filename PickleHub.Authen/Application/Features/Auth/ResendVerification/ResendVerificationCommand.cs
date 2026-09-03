using PickleHub.Common.Interfaces;
using MassTransit;
using MediatR;
using PickleHub.Authen.Domain.Entities;
using PickleHub.Authen.Domain.Repositories;
using PickleHub.Authen.Infrastructure.Service;
using PickleHub.Common.Events.Authen;

namespace PickleHub.Authen.Application.Features.Auth.ResendVerification
{
    public record ResendVerificationCommand(string Email) : IRequest;

    public class ResendVerificationHandler : IRequestHandler<ResendVerificationCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailVerificationTokenRepository _verificationTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IConfiguration _config;
        private readonly JwtTokenService _jwtService;
        public ResendVerificationHandler(
            IUserRepository userRepository,
            IEmailVerificationTokenRepository verificationTokenRepository,
            IUnitOfWork unitOfWork,
            JwtTokenService jwtService,
            IPublishEndpoint publishEndpoint,
            IConfiguration config)
        {
            _userRepository = userRepository;
            _verificationTokenRepository = verificationTokenRepository;
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _publishEndpoint = publishEndpoint;
            _config = config;
        }

        public async Task Handle(ResendVerificationCommand request, CancellationToken ct)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, ct);
            if (user == null || user.IsEmailVerified) return;

            // vo hieu hoa cac token cu
            var oldTokens = await _verificationTokenRepository.GetActiveByUserIdAsync(user.Id, ct);

            foreach (var old in oldTokens)
            {
                old.MarkAsUsed();
            }

            // tao token moi
            var tokenValue = _jwtService.GenerateEmailVerificationToken();
            var newToken = EmailVerificationToken.Create(user.Id, tokenValue);
            _verificationTokenRepository.Add(newToken);
            await _unitOfWork.SaveChangesAsync();

            var verifyLink = $"{_config["App:BaseUrl"]}/verify-email?token={tokenValue}";
            await _publishEndpoint.Publish(new UserRegisteredEvent
            {
                UserId = user.Id,
                Email = user.Email,
                CustomerName = user.Email.Split('@')[0],
                VerificationToken = tokenValue,
                VerificationUrl = verifyLink,
                RegisteredAt = DateTime.UtcNow
            }, ct);

        }
    }
}

