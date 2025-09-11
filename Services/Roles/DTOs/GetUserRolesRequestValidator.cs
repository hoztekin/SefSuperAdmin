using FluentValidation;

namespace App.Services.Roles.DTOs
{
    public class GetUserRolesRequestValidator : AbstractValidator<GetUserRolesRequest>
    {
        public GetUserRolesRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id boş olamaz.");
        }
    }
}
