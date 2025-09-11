using FluentValidation;

namespace App.Services.Users.Create
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.EMail).NotEmpty().WithMessage("Mail Adresi zorunludur").EmailAddress().WithMessage("Mail adresi hatalı");

            RuleFor(x => x.Password).NotEmpty().WithMessage("Parola alanı zorunludur").MinimumLength(4).WithMessage("Parolanız minimum 4 karakter olmalıdır");

            RuleFor(x => x.UserName).NotEmpty().WithMessage("Kullanıcı adı zorunludur.");

            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Parolalar eşleşmiyor").When(x => !string.IsNullOrEmpty(x.Password));

        }
    }
}
