using MediatR;
using PickleHub.Catalog.Application.Features.Brands.DTOs;
using PickleHub.Catalog.Domain.Repositories;

namespace PickleHub.Catalog.Application.Features.Brands.GetBrand
{
    public record GetBrandsQuery : IRequest<List<BrandDto>>;

    public class GetBrandsHandler : IRequestHandler<GetBrandsQuery, List<BrandDto>>
    {
        private readonly IBrandRepository _brandRepository;
        public GetBrandsHandler(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository;
        }
        public async Task<List<BrandDto>> Handle(GetBrandsQuery request, CancellationToken ct)
        {
            var brands = await _brandRepository.GetAllAsync(ct);
            return brands.Select(b => new BrandDto
            { 
                Id = b.Id,
                Name = b.Name, 
                Slug = b.Slug.Value
            }).ToList();
        }
    }
}
