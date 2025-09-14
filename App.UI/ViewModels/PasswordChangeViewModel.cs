using System.ComponentModel.DataAnnotations;

namespace App.UI.ViewModels;

public class PasswordChangeViewModel
{

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Eski Şifre Zorunludur")]
    [Display(Name = "Eski Şifreniz")]
    [MinLength(4, ErrorMessage = "Şifreniz en az 4 karakter olmalıdır")]
    public string OldPassword { get; set; }


    [Required(ErrorMessage = "Yeni Şifre Zorunludur")]
    [Display(Name = "Yeni Şifreniz")]
    [MinLength(4, ErrorMessage = "Şifreniz en az 4 karakter olmalıdır")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }



    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Yeni Şifre Tekrarı Zorunludur")]
    [Display(Name = "Yeni Şifreniz")]
    [MinLength(4, ErrorMessage = "Şifreniz en az 4 karakter olmalıdır")]
    [Compare("NewPassword", ErrorMessage = "Şifreniz eşleşmemektedir")]
    public string ConfirmNewPassword { get; set; }

    public string UserId { get; set; }

}


