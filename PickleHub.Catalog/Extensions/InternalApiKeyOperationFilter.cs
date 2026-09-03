using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using PickleHub.Common.Constants;

namespace PickleHub.Catalog.Extensions
{
    public class InternalApiKeyOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null || context.ApiDescription.RelativePath == null)
            {
                return;
            }

            if (!context.ApiDescription.RelativePath.Contains("internal/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "InternalApiKey"
                        }
                    },
                    new List<string>()
                }
            });
        }
    }
}
