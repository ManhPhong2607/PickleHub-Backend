using PickleHub.Common.Domain;

namespace PickleHub.System.Domain.Entities
{
    public class SystemConfig : BaseEntity
    {
        public string Key { get; private set; } = string.Empty;
        public string Value { get; private set; } = string.Empty;
        public string? Description { get; private set; }

        private SystemConfig() { }

        public static SystemConfig Create(string key, string value, string? description = null)
        {
            return new SystemConfig
            {
                Key = key.Trim().ToLowerInvariant(),
                Value = value.Trim(),
                Description = description?.Trim()
            };
        }

        public void Update(string value, string? description = null)
        {
            Value = value.Trim();
            Description = description?.Trim();
            SetUpdated();
        }
    }
}
