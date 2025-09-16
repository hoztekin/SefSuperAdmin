namespace App.UI.Application.DTOS
{
    public class UpdateUserDtoUI
    {
        public string UserName { get; set; }
        public string EMail { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
