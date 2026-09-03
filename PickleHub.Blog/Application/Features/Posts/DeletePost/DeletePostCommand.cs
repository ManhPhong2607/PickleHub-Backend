using MediatR;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Blog.Application.Features.Posts.DeletePost
{
    public record DeletePostCommand(Guid Id) : IRequest;
    public class DeletePostHandler : IRequestHandler<DeletePostCommand>
    {
        private readonly IPostRepository _postRepository;
        private readonly IUnitOfWork _unitOfWork;
        public  DeletePostHandler(IPostRepository postRepository, IUnitOfWork unitOfWork)
        {
            _postRepository = postRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(DeletePostCommand request, CancellationToken ct)
        {
            var post = await _postRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Không tìm thấy bài viết.");

            if(!post.CanBeDeleted)
                throw new ConflictException("Không thể xóa bài viết đang ở trạng thái Published. Vui lòng lưu trữ (Archive) bài viết trước khi xóa.");

            _postRepository.Remove(post);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

}
