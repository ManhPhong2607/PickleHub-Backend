using PickleHub.Common.Interfaces;
using PickleHub.Common.Events.Customers;
using MassTransit;
using PickleHub.Authen.Domain.Repositories;

namespace PickleHub.Authen.Infrastructure.Consumers
{
    public class CustomerBlockedConsumer : IConsumer<CustomerBlockedEvent>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CustomerBlockedConsumer(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<CustomerBlockedEvent> context)
        {
            var message = context.Message;

            var user = await _userRepository.GetByIdAsync(
                message.UserId,
                context.CancellationToken);

            if (user == null) return;

            if (message.IsBlocked)
            {
                user.Block();

                var activeTokens = await _refreshTokenRepository
                    .GetActiveByUserIdAsync(user.Id, context.CancellationToken);

                foreach (var token in activeTokens)
                    token.Revoke();
            }
            else
            {
                user.UnBlock();
            }

            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
