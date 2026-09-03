using PickleHub.Common.Interfaces;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Application.DTOs;
using PickleHub.Authen.Domain.Entities;
using PickleHub.Authen.Infrastructure.Persistence;
using PickleHub.Authen.Infrastructure.Service;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Events.Authen;
using PickleHub.Authen.Domain.Repositories;

namespace PickleHub.Authen.Application.Features.Auth.Register
{
    public record RegisterCommand(string Email, string Password) : IRequest<RegisterResultDto>;

    public class RegisterHandler : IRequestHandler<RegisterCommand, RegisterResultDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailVerificationTokenRepository _verificationTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtTokenService _jwtService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IConfiguration _config;

        public RegisterHandler(
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
        public async Task<RegisterResultDto> Handle(
            RegisterCommand request, CancellationToken ct)
        {
            var existed = await _userRepository.ExistsByEmailAsync(request.Email, ct);
            if (existed)
                throw new ConflictException("Email này đã được đăng ký.");

            var user = User.Create(
                request.Email,
                BCrypt.Net.BCrypt.HashPassword(request.Password));

            _userRepository.Add(user);
            await _unitOfWork.SaveChangesAsync(ct);

            // Tạo email verification token
            var tokenValue = _jwtService.GenerateEmailVerificationToken();
            var verificationToken = EmailVerificationToken.Create(user.Id, tokenValue);
            _verificationTokenRepository.Add(verificationToken);
            await _unitOfWork.SaveChangesAsync(ct);

            var verifyLink = $"{_config["App:BaseUrl"]}/verify-email?token={tokenValue}";

            // Publish event để Notification Service gửi email và Customer Service tạo customer record
            await _publishEndpoint.Publish(new UserRegisteredEvent
            {
                UserId = user.Id,
                Email = user.Email,
                CustomerName = user.Email.Split('@')[0],
                VerificationToken = tokenValue,
                VerificationUrl = verifyLink,
                RegisteredAt = DateTime.UtcNow
            }, ct);

            return new RegisterResultDto(
                "Đăng ký thành công. Vui lòng kiểm tra email để xác minh tài khoản.",
                user.Email);
        }
    }
}


