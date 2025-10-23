using System.ComponentModel.DataAnnotations;

namespace App.UI.Application.DTOS
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Kullanıcı ID'si gereklidir")]
        public string Id { get; set; }

        [Required(ErrorMessage = "Yeni parola gereklidir")]
        [MinLength(6, ErrorMessage = "Parola en az 6 karakter olmalıdır")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Parola tekrarı gereklidir")]
        [Compare("NewPassword", ErrorMessage = "Parolalar eşleşmiyor")]
        public string ConfirmNewPassword { get; set; }
    }
}
