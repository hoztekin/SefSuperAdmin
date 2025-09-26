using App.UI.Application.Enums;

namespace App.UI.Application.DTOS
{
    public class CreateExternalUserDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public UserLoginType LoginType { get; set; } = UserLoginType.Shared;
    }
}
