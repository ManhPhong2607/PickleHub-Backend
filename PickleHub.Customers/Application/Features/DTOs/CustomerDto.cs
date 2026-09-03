namespace PickleHub.Customers.Application.Features.DTOs
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerSummaryDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerInternalDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public decimal LoyaltyDiscountPercent { get; set; }
    }
}
