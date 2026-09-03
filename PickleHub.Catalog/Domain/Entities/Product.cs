using PickleHub.Catalog.Domain.Enums;
using PickleHub.Common.Domain;
using PickleHub.Common.Exceptions;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Catalog.Domain.Entities
{
    public class Product : BaseEntity
    {
        public Guid CategoryId { get; private set; }
        public Guid BrandId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public Slug Slug { get; private set; } = null!;
        public string Description { get; private set; } = string.Empty;
        // Computed từ Variant.Price (MIN của các variant hiện có). Không truyền qua constructor/param, không có API nào set trực tiếp.
        // = 0 khi Product chưa có variant nào (chỉ xảy ra ở trạng thái Draft, vì Publish() bắt buộc >= 1 variant).
        public decimal BasePrice { get; private set; }
        public ProductStatus Status { get; private set; } = ProductStatus.Draft;
        public string SpecsJson { get; private set; } = "{}";
        public int SoldCount { get; private set; }
        public int ViewCount { get; private set; }

        public Category? Category { get; private set; }
        public Brand? Brand { get; private set; }

        private readonly List<ProductImage> _images = new();
        public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

        private readonly List<ProductVariant> _variants = new();
        public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

        private Product() { }

        // Factory method 
        public static Product Create(string name, Slug slug, string description,
            Guid categoryId, Guid brandId, string specsJson)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Tên sản phẩm không được để trống.");

            return new Product
            {
                Name = name,
                Slug = slug,
                Description = description,
                CategoryId = categoryId,
                BrandId = brandId,
                BasePrice = 0,
                SpecsJson = string.IsNullOrWhiteSpace(specsJson) ? "{}" : specsJson,
                Status = ProductStatus.Draft
            };
        }

        public void Update(string name, Slug slug, string description,
            Guid categoryId, Guid brandId, string specsJson)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Tên sản phẩm không được để trống.");

            Name = name;
            Slug = slug;
            Description = description;
            CategoryId = categoryId;
            BrandId = brandId;
            SpecsJson = string.IsNullOrWhiteSpace(specsJson) ? "{}" : specsJson;
            UpdatedAt = DateTime.UtcNow;
        }

        //  Hành vi thay đổi trạng thái 
        public void Publish()
        {
            if (Status == ProductStatus.Hidden)
                throw new DomainException("Không thể publish sản phẩm đã bị ẩn. Vui lòng khôi phục trước.");

            if (!_images.Any())
                throw new DomainException("Sản phẩm phải có ít nhất 1 ảnh trước khi publish.");

            if (!_variants.Any())
                throw new DomainException("Sản phẩm phải có ít nhất 1 biến thể trước khi publish.");

            Status = ProductStatus.Active;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Hide()
        {
            if (Status == ProductStatus.Draft)
                throw new DomainException("Sản phẩm đang ở trạng thái Draft, không cần ẩn.");

            Status = ProductStatus.Hidden;
            SetUpdated();
        }

        public void Restore()
        {
            if (Status != ProductStatus.Hidden)
                throw new DomainException("Chỉ có thể khôi phục sản phẩm đang bị ẩn.");

            Status = ProductStatus.Draft;
            SetUpdated();
        }

        // image/video
      
        private const int MaxImagesPerGroup = 7;
        private const int MaxSizeChartImagesPerGroup = 1;
        public void EnsureCanAddImages(Guid? variantId, bool isSizeChart, int count)
        {
            if (variantId.HasValue && !_variants.Any(v => v.Id == variantId.Value))
                throw new NotFoundException("Biến thể không tồn tại trong sản phẩm này.");

            var currentCount = _images.Count(i => i.VariantId == variantId && i.IsSizeChart == isSizeChart);
            var maxAllowed = isSizeChart ? MaxSizeChartImagesPerGroup : MaxImagesPerGroup;

            if (currentCount + count > maxAllowed)
            {
                var groupName = variantId.HasValue ? "biến thể" : "sản phẩm";
                var imageType = isSizeChart ? "ảnh size chart" : "ảnh";
                var remaining = Math.Max(0, maxAllowed - currentCount);
                throw new DomainException(
                    $"Mỗi {groupName} chỉ được tối đa {maxAllowed} {imageType} (còn lại {remaining} chỗ trống, bạn đang tải lên {count}).");
            }
        }
        public ProductImage AddImage(
            string publicId,
            string url,
            //int sortOrder,
            Guid? variantId = null,
            bool isSizeChart = false)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new DomainException("URL ảnh không được để trống.");

            if (string.IsNullOrWhiteSpace(publicId))
                throw new DomainException("PublicId ảnh không được để trống.");

            if (variantId.HasValue && !_variants.Any(v => v.Id == variantId.Value))
                throw new NotFoundException("Biến thể không tồn tại trong sản phẩm này.");

            var group = _images.Where(i => i.VariantId == variantId && i.IsSizeChart == isSizeChart);
            var maxAllowed = isSizeChart ? MaxSizeChartImagesPerGroup : MaxImagesPerGroup;
            if (group.Count() >= maxAllowed)
            {
                var groupName = variantId.HasValue ? "biến thể" : "sản phẩm";
                var imageType = isSizeChart ? "ảnh size chart" : "ảnh";
                throw new DomainException($"Mỗi {groupName} chỉ được tối đa {maxAllowed} {imageType}.");
            }
            var nextSortOrder = group.Select(i => i.SortOrder).DefaultIfEmpty(-1).Max() + 1;

            var image = ProductImage.Create(Id, publicId, url, nextSortOrder, variantId, isSizeChart);
            _images.Add(image);
            UpdatedAt = DateTime.UtcNow;
            return image;
        }

        public void RemoveImage(Guid imageId)
        {
            var image = _images.FirstOrDefault(i => i.Id == imageId);
            if (image == null)
                throw new NotFoundException("Ảnh không tồn tại trong sản phẩm này.");

            if (Status == ProductStatus.Active && _images.Count == 1)
                throw new DomainException("Không thể xóa ảnh cuối cùng của sản phẩm đang published. Vui lòng ẩn sản phẩm trước.");
            _images.Remove(image);
            SetUpdated();
        }

        public (ProductImage NewImage, string OldPublicId) ReplaceImage(Guid oldImageId, string newPublicId, string newUrl)
        {
            var oldImage = _images.FirstOrDefault(i => i.Id == oldImageId)
                ?? throw new NotFoundException("Ảnh không tồn tại trong sản phẩm này.");

            if (string.IsNullOrWhiteSpace(newUrl))
                throw new DomainException("URL ảnh không được để trống.");
            if (string.IsNullOrWhiteSpace(newPublicId))
                throw new DomainException("PublicId ảnh không được để trống.");

            var newImage = ProductImage.Create(Id, newPublicId, newUrl, oldImage.SortOrder, oldImage.VariantId, oldImage.IsSizeChart);
            _images.Remove(oldImage);
            _images.Add(newImage);
            UpdatedAt = DateTime.UtcNow;
            return (newImage, oldImage.PublicId);
        }

        public ProductVariant AddVariant(string sku, string attributesJson, decimal price)
        {
            if (_variants.Any(v => v.Sku == sku))
                throw new ConflictException($"SKU '{sku}' đã tồn tại trong sản phẩm này.");

            var variant = ProductVariant.Create(Id, sku, attributesJson, price);
            _variants.Add(variant);
            RecalculateBasePrice();
            SetUpdated();
            return variant;
        }

        public void UpdateVariant(Guid variantId, string sku, string attributesJson, decimal price)
        {
            var variant = _variants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null)
                throw new NotFoundException("Biến thể không tồn tại trong sản phẩm này.");

            if (_variants.Any(v => v.Sku == sku && v.Id != variantId))
                throw new ConflictException($"SKU '{sku}' đã tồn tại trong sản phẩm này.");

            variant.Update(sku, attributesJson, price);
            RecalculateBasePrice();
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveVariant(Guid variantId)
        {
            var variant = _variants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null)
                throw new NotFoundException("Biến thể không tồn tại trong sản phẩm này.");

            if (Status == ProductStatus.Active && _variants.Count == 1)
                throw new DomainException("Không thể xóa biến thể cuối cùng của sản phẩm đang published. Vui lòng ẩn sản phẩm trước.");
            _variants.Remove(variant);
            RecalculateBasePrice();
            SetUpdated();
        }

        public void IncreaseSoldCount(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Số lượng bán phải lớn hơn 0.");

            SoldCount += quantity;
            SetUpdated();
        }

        public void IncreaseViewCount()
        {
            ViewCount++;        
        }

        private void RecalculateBasePrice()
        {
            BasePrice = _variants.Any() ? _variants.Min(v => v.Price) : 0;
        }
    }
}
