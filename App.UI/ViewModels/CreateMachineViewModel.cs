using System.ComponentModel.DataAnnotations;

namespace App.UI.ViewModels
{
    public class CreateMachineViewModel
    {
        [Required(ErrorMessage = "Şube ID'si gereklidir")]
        [MaxLength(50, ErrorMessage = "Şube ID'si en fazla 50 karakter olabilir")]
        public string BranchId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şube adı gereklidir")]
        [MaxLength(100, ErrorMessage = "Şube adı en fazla 100 karakter olabilir")]
        public string BranchName { get; set; } = string.Empty;

        [Required(ErrorMessage = "API adresi gereklidir")]
        [MaxLength(200, ErrorMessage = "API adresi en fazla 200 karakter olabilir")]
        [Url(ErrorMessage = "Geçerli bir URL formatı giriniz")]
        public string ApiAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Makine kodu gereklidir")]
        [MaxLength(20, ErrorMessage = "Makine kodu en fazla 20 karakter olabilir")]
        public string Code { get; set; } = string.Empty;
    }
}
