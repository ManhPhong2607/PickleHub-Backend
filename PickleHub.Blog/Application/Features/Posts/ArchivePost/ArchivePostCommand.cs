using MediatR;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Blog.Application.Features.Posts.ArchivePost
{
    public record ArchivePostCommand (Guid Id) : IRequest;

    public class ArchivePostHandler : IRequestHandler<ArchivePostCommand>
    {
        private readonly IPostRepository _postRepository;
        private readonly IUnitOfWork _unitOfWork;
        public ArchivePostHandler(IPostRepository postRepository, IUnitOfWork unitOfWork)
        {
            _postRepository = postRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(ArchivePostCommand request, CancellationToken ct)
        {
            var post = await _postRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Không tìm thấy bài viết.");

            post.Archive();
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
