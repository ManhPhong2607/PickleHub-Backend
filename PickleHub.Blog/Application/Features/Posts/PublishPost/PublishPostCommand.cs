using MediatR;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Blog.Application.Features.Posts.PublishPost
{
    public record PublishPostCommand(Guid Id) : IRequest;

    public class PublishPostHandler : IRequestHandler<PublishPostCommand>
    {
        private readonly IPostRepository _postRepository;
        private readonly IUnitOfWork _unitOfWork;
        public PublishPostHandler(IPostRepository postRepository,IUnitOfWork unitOfWork) 
        {
            _postRepository = postRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(PublishPostCommand request, CancellationToken ct)
        {
            var post = await _postRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Không tìm thấy bài viết.");

            post.Publish();
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
