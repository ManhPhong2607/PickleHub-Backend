using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Authen.Application.DTOs;
using PickleHub.Authen.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Authen.Application.Features.Auth.VerifyEmail
{
    public record VerifyEmailCommand(string Token) : IRequest<VerifyEmailResultDto>;

    public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResultDto>
    {
        private readonly IEmailVerificationTokenRepository _verificationTokenRepository;
        private readonly IUnitOfWork _unitOfWork;

        public VerifyEmailHandler(
            IEmailVerificationTokenRepository verificationTokenRepository,
            IUnitOfWork unitOfWork)
        {
            _verificationTokenRepository = verificationTokenRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<VerifyEmailResultDto> Handle(
            VerifyEmailCommand request, CancellationToken ct)
        {
            var token = await _verificationTokenRepository
                .GetByTokenAsync(request.Token, ct);

            if (token is null || !token.IsValid)
                throw new UnauthorizedException(
                    "Link xác minh không hợp lệ hoặc đã hết hạn.");

            if (token.User is null)
                throw new NotFoundException("Không tìm thấy người dùng.");

            if (token.User.IsEmailVerified)
                return new VerifyEmailResultDto("Email đã được xác minh trước đó.");

            token.User.VerifyEmail();
            token.MarkAsUsed();

            await _unitOfWork.SaveChangesAsync(ct);

            return new VerifyEmailResultDto(
                "Xác minh email thành công. Bạn có thể đăng nhập ngay bây giờ.");
        }
    }
}
