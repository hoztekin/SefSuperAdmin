using System.ComponentModel.DataAnnotations;

namespace App.UI.Application.DTOS
{
    public class CompanyCreateDto
    {
        [Required(ErrorMessage = "Şirket adı zorunludur")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Şirket adı 2 ile 255 karakter arasında olmalıdır")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Şirket kodu zorunludur")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Şirket kodu 2 ile 50 karakter arasında olmalıdır")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Vergi numarası zorunludur")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Vergi numarası 2 ile 20 karakter arasında olmalıdır")]
        public string TaxNumber { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string Phone { get; set; }

        public string LogoPath { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        [Required(ErrorMessage = "İlçe seçimi zorunludur")]
        public Guid DistrictId { get; set; }
    }
}
