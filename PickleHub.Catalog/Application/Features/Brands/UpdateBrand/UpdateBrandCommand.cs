using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Catalog.Application.Features.Brands.DTOs;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Catalog.Application.Features.Brands.UpdateBrand
{
    public record UpdateBrandCommand(Guid BrandId, string Name) : IRequest<BrandDto>;
    public class UpdateBrandHandler : IRequestHandler<UpdateBrandCommand, BrandDto>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IUnitOfWork _unitOfWork;
        public UpdateBrandHandler(IBrandRepository brandRepository, IUnitOfWork unitOfWork)
        {
            _brandRepository = brandRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<BrandDto> Handle(UpdateBrandCommand request, CancellationToken ct)
        {
            var brand = await _brandRepository.GetByIdAsync(request.BrandId, ct)
                ?? throw new NotFoundException($"Không tìm thấy thương hiệu với Id: {request.BrandId}");

            var slug = await GenerateUniqueSlugAsync(request.Name, request.BrandId, ct);
            brand.Update(request.Name, slug);
            _brandRepository.Update(brand);
            await _unitOfWork.SaveChangesAsync(ct);

            return new BrandDto
            {
                Id = brand.Id,
                Name = brand.Name,
                Slug = brand.Slug.Value
            };
        }

        private async Task<Slug> GenerateUniqueSlugAsync(string name, Guid? excludeId, CancellationToken ct)
        {
            var baseSlug = Slug.Create(name);
            var candidate = baseSlug;
            var counter = 1;
            while (await _brandRepository.ExistsBySlugAsync(candidate.Value, excludeId, ct))
                candidate = baseSlug.AppendSuffix(counter++);

            return candidate;
        }
    }
}
