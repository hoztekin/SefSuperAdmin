using App.Services.Machine.Dtos;
using FluentValidation;

namespace App.Services.Machine.Validation
{
    public class CreateMachineDtoValidator : AbstractValidator<CreateMachineDto>
    {
        public CreateMachineDtoValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("Şube ID'si gereklidir")
                .MaximumLength(50)
                .WithMessage("Şube ID'si en fazla 50 karakter olabilir");

            RuleFor(x => x.BranchName)
                .NotEmpty()
                .WithMessage("Şube adı gereklidir")
                .MaximumLength(100)
                .WithMessage("Şube adı en fazla 100 karakter olabilir");

            RuleFor(x => x.ApiAddress)
                .NotEmpty()
                .WithMessage("API adresi gereklidir")
                .MaximumLength(200)
                .WithMessage("API adresi en fazla 200 karakter olabilir")
                .Must(BeValidUrl)
                .WithMessage("Geçerli bir URL formatı giriniz");

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Makine kodu gereklidir")
                .MaximumLength(20)
                .WithMessage("Makine kodu en fazla 20 karakter olabilir");
        }

        private static bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}
