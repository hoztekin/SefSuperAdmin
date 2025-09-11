using App.Services.Authentications.Login;
using FluentValidation;

namespace App.Services.Authentications.DTOs
{
    public class ClientLoginDtoValidator : AbstractValidator<ClientLoginDto>
    {
        public ClientLoginDtoValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty().WithMessage("Client ID zorunludur.").NotNull().WithMessage("Client ID zorunludur.");

            RuleFor(x => x.ClientSecret).NotEmpty().WithMessage("Client şifresi zorunludur.").NotNull().WithMessage("Client şifresi zorunludur.");

        }
    }
}
