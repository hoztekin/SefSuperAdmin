namespace App.Services.Account.Dtos;

public record PasswordChangeDTO(string OldPassword, string NewPassword, string ConfirmNewPassword, string UserId);


