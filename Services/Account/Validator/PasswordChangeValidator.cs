using App.Services.Account.Dtos;
using FluentValidation;

namespace App.Services.Account.Validator
{
    internal class PasswordChangeValidator : AbstractValidator<PasswordChangeDTO>
    {
        public PasswordChangeValidator()
        {
            RuleFor(x => x.OldPassword).NotEmpty().WithMessage("Parola alanı zorunludur").MinimumLength(4).WithMessage("Parolanız minimum 4 karakter olmalıdır");

            RuleFor(x => x.NewPassword).NotEmpty().WithMessage("Parola alanı zorunludur").MinimumLength(4).WithMessage("Parolanız minimum 4 karakter olmalıdır");

            RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("Parolalar eşleşmiyor").When(x => !string.IsNullOrEmpty(x.ConfirmNewPassword));
        }
    }

}
