using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PickleHub.Common.DTOs
{
    public class ErrorResponseDto
    {
        public ErrorDetail Error { get; set; } = new();
    }

    public class ErrorDetail
    {

        public string Message { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string[]>? Errors { get; set; }
    }
}
