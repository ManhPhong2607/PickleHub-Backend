using PickleHub.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Domain.Enums;
using PickleHub.Authen.Domain.Repositories;
using PickleHub.Authen.Infrastructure.Persistence;
using PickleHub.Common.Exceptions;

namespace PickleHub.Authen.Application.Features.Auth.BlockUser
{
    public record BlockUserCommand(Guid UserId, bool IsBlocked) : IRequest;

    public class BlockUserHandler : IRequestHandler<BlockUserCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BlockUserHandler(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(BlockUserCommand request, CancellationToken ct)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, ct)
                ?? throw new NotFoundException("Không tìm thấy người dùng.");

            if (user.Role == UserRole.Admin)
                throw new ForbiddenException("Không thể block tài khoản Admin.");

            if (request.IsBlocked)
            {
                user.Block();

                // Thu hồi toàn bộ refresh token ngay khi block
                var activeTokens = await _refreshTokenRepository
                    .GetActiveByUserIdAsync(user.Id, ct);

                foreach (var t in activeTokens)
                    t.Revoke();
            }
            else
            {
                user.UnBlock();
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
