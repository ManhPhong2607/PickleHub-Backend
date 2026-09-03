using MediatR;
using PickleHub.Catalog.Application.Features.Categories.DTOs;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Categories.UpdateCategoryAttributeSchema
{
    public record UpdateCategoryAttributeSchemaCommand(Guid Id, string AttributeSchemaJson) : IRequest<CategoryTreeDto>;

    public class UpdateCategoryAttributeSchemaCommandHandler : IRequestHandler<UpdateCategoryAttributeSchemaCommand, CategoryTreeDto>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCategoryAttributeSchemaCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CategoryTreeDto> Handle(UpdateCategoryAttributeSchemaCommand request, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(request.Id, ct)
                   ?? throw new NotFoundException("Danh mục không tồn tại");
            ValidateSchemaJson(request.AttributeSchemaJson);
            category.UpdateAttributeSchema(request.AttributeSchemaJson);
            _categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync(ct);

            return new CategoryTreeDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug.Value,
                ParentId = category.ParentId,
                AttributeSchemaJson = category.AttributeSchemaJson,
            };
        }

        // Chỉ validate đây là JSON array hợp lệ, không ép cấu trúc từng phần tử quá chặt ở backend, vì admin FE là nơi kiểm soát UI form, backend chỉ cần đảm bảo dữ liệu
        private static void ValidateSchemaJson(string json)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                {
                    throw new DomainException("AttributeSchemaJson phải là một mảng JSON");
                }
            }
            catch (System.Text.Json.JsonException)
            {
                throw new DomainException("AttributeSchemaJson không phải là JSON hợp lệ");
            }
        }
    }
}
