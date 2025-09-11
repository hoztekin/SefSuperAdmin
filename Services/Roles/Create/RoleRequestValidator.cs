using FluentValidation;

namespace App.Services.Roles.Create
{
    public class RoleRequestValidator : AbstractValidator<RoleRequest>
    {
        public RoleRequestValidator()
        {
            RuleFor(x => x.RoleName).NotEmpty().WithMessage("Rol alanı boş olamaz.");
        }
    }
}
