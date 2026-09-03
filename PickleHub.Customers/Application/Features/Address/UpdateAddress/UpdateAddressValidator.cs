using FluentValidation;

namespace PickleHub.Customers.Application.Features.Address.UpdateAddress
{
    public class UpdateAddressValidator : AbstractValidator<UpdateAddressCommand>
    {
        public UpdateAddressValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Vui lòng nhập họ tên.")
                .MaximumLength(200);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$")
                .WithMessage("Số điện thoại không hợp lệ.");

            RuleFor(x => x.Province)
                 .NotEmpty().WithMessage("Vui lòng chọn tỉnh/thành phố.");

            RuleFor(x => x.District)
                .NotEmpty().WithMessage("Vui lòng chọn quận/huyện.");

            RuleFor(x => x.Ward)
                .NotEmpty().WithMessage("Vui lòng chọn phường/xã.");

            RuleFor(x => x.StreetAddress)
                .NotEmpty().WithMessage("Vui lòng nhập địa chỉ cụ thể.")
                .MaximumLength(300)
                .WithMessage("Địa chỉ không được vượt quá 300 ký tự.");
        }
    }
}
