using App.Services.Authentications.Login;
using FluentValidation;

namespace App.Services.Authentications.DTOs
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.UserName).NotEmpty().WithMessage("Kullanıcı adı zorunludur.").NotNull().WithMessage("Kullanıcı adı zorunludur.");

            RuleFor(x => x.Password).NotEmpty().WithMessage("Şifre alanı zorunludur.").NotNull().WithMessage("Şifre alanı zorunludur.");

        }
    }
}
