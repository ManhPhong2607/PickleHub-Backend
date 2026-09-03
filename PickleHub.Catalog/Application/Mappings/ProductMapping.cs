using PickleHub.Catalog.Application.Features.Brands.DTOs;
using PickleHub.Catalog.Application.Features.Categories.DTOs;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Entities;

namespace PickleHub.Catalog.Application.Mappings
{
    public static class ProductMapping
    {
        public static ProductDetailDto MapToDetailDto(this Product product, PromotionBadgeDto? activePromotion = null) 
        {
            var discountPercent = activePromotion?.DiscountPercent ?? 0m;
            return new()
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug.Value,
                Description = product.Description,
                BasePrice = product.BasePrice,
                EffectivePrice = discountPercent > 0
                    ? Math.Round(product.BasePrice * (1 - discountPercent / 100m), 0)
                    : product.BasePrice,
                IsOnSale = discountPercent > 0,
                SalePercent = discountPercent > 0 ? discountPercent : null,
                SaleStartsAt = activePromotion?.StartsAt,
                SaleEndsAt = activePromotion?.EndsAt,
                Status = product.Status.ToString(),
                SpecsJson = product.SpecsJson,
                SoldCount = product.SoldCount,
                Category = product.Category is null ? null : new CategorySummaryDto
                {
                    Id = product.Category.Id,
                    Name = product.Category.Name,
                    Slug = product.Category.Slug.Value
                },
                Brand = product.Brand is null ? null : new BrandDto
                {
                    Id = product.Brand.Id,
                    Name = product.Brand.Name,
                    Slug = product.Brand.Slug.Value
                },
                Images = product.Images
                 .OrderBy(i => i.SortOrder)
                 .Select(i => new ProductImageDto
                 {
                     Id = i.Id,
                     PublicId = i.PublicId,
                     Url = i.Url,
                     VariantId = i.VariantId,
                     SortOrder = i.SortOrder,
                     IsSizeChart = i.IsSizeChart
                 }).ToList(),
                Variants = product.Variants
                 .OrderBy(v => v.CreatedAt)
                 .Select(v =>
                 {
                     var ownImages = product.Images
                         .Where(i => i.VariantId == v.Id && !i.IsSizeChart)
                         .OrderBy(i => i.SortOrder)
                         .ToList();

                     var images = ownImages.Count > 0
                         ? ownImages
                         : product.Images
                             .Where(i => i.VariantId == null && !i.IsSizeChart)
                             .OrderBy(i => i.SortOrder)
                             .ToList();

                     var image = images.FirstOrDefault();

                     return new ProductVariantDto
                     {
                         Id = v.Id,
                         ProductId = product.Id,
                         ProductName = product.Name,
                         Sku = v.Sku,
                         AttributesJson = v.AttributesJson,
                         Price = v.Price,
                         EffectivePrice = discountPercent > 0
                             ? Math.Round(v.Price * (1 - discountPercent / 100m), 0)
                             : v.Price,
                         ImageUrl = image?.Url,
                         Images = images.Select(i => new ProductImageDto
                         {
                             Id = i.Id,
                             PublicId = i.PublicId,
                             Url = i.Url,
                             VariantId = i.VariantId,
                             SortOrder = i.SortOrder,
                             IsSizeChart = i.IsSizeChart
                         }).ToList()
                     };
                 }).ToList()
            };
        }

        // Ảnh đại diện dùng cho danh sách/card sản phẩm. Ưu tiên: ảnh chung (VariantId = null).
        // Nếu Admin chưa upload ảnh chung, fallback về ảnh của variant được upload sớm nhất (theo CreatedAt)
        // mà không cần JOIN/Include(p => p.Variants) trong các query danh sách.
        public static string? ResolveThumbnailUrl(this Product product)
        {
            if (product.Images is null || !product.Images.Any())
                return null;

            var generalImage = product.Images
                .Where(i => !i.IsSizeChart && i.VariantId == null)
                .OrderBy(i => i.SortOrder)
                .Select(i => i.Url)
                .FirstOrDefault();

            if (generalImage is not null)
                return generalImage;

            return product.Images
                .Where(i => !i.IsSizeChart && i.VariantId != null)
                .OrderBy(i => i.CreatedAt)
                .ThenBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .Select(i => i.Url)
                .FirstOrDefault();
        }
    }
}
