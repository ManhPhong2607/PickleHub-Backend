using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Catalog.Application.Features.Brands.DTOs;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Catalog.Application.Features.Brands.CreateBrand
{
    public record CreateBrandCommand(string Name) : IRequest<BrandDto>;

    public class CreateBrandHandler : IRequestHandler<CreateBrandCommand, BrandDto>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IUnitOfWork _unitOfWork;
        public CreateBrandHandler(IBrandRepository brandRepository, IUnitOfWork unitOfWork)
        {
            _brandRepository = brandRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<BrandDto> Handle(CreateBrandCommand request, CancellationToken ct)
        {
            var slug = await GenerateUniqueSlugAsync(request.Name, null, ct);
            var brand = Brand.Create(request.Name, slug);
            _brandRepository.Add(brand);
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

            while(await _brandRepository.ExistsBySlugAsync(candidate.Value, excludeId, ct))
                candidate = baseSlug.AppendSuffix(counter++);

            return candidate;
        }
    }
}
