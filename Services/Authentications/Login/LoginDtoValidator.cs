using App.Services.Authentications.Login;
using FluentValidation;

namespace App.Services.Authentications.DTOs
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Mail adresi zorunludur.").NotNull().WithMessage("Mail adresi zorunludur.");

            RuleFor(x => x.Password).NotEmpty().WithMessage("Şifre alanı zorunludur.").NotNull().WithMessage("Şifre alanı zorunludur.");

        }
    }
}
