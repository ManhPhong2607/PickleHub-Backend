using PickleHub.Common.Domain;

namespace PickleHub.Customers.Domain.Entities
{
    public class CustomerAddress : BaseEntity
    {
        public Guid CustomerId { get; private set; }
        public string FullName { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;
        public string Province { get; private set; } = string.Empty;
        public string District { get; private set; } = string.Empty;
        public string Ward { get; private set; } = string.Empty;
        public string StreetAddress { get; private set; } = string.Empty;
        public bool IsDefault { get; private set; } = false;

        private CustomerAddress() { }

        public static CustomerAddress Create(
            Guid customerId,
            string fullName,
            string phoneNumber,
            string province,
            string district,
            string ward,
            string streetAddress,
            bool isDefault = false)
        {
            return new CustomerAddress
            {
                CustomerId = customerId,
                FullName = fullName.Trim(),
                PhoneNumber = phoneNumber.Trim(),
                Province = province.Trim(),
                District = district.Trim(),
                Ward = ward.Trim(),
                StreetAddress = streetAddress.Trim(),
                IsDefault = isDefault
            };
        }

        public void Update(
            string fullName,
            string phoneNumber,
            string province,
            string district,
            string ward,
            string streetAddress)
        {
            FullName = fullName.Trim();
            PhoneNumber = phoneNumber.Trim();
            Province = province.Trim();
            District = district.Trim();
            Ward = ward.Trim();
            StreetAddress = streetAddress.Trim();
            SetUpdated();
        }

        public void SetAsDefault()
        {
            IsDefault = true;
            SetUpdated();
        }

        public void UnsetDefault()
        {
            IsDefault = false;
            SetUpdated();
        }
    }
}
