using App.UI.Application.Enums;

namespace App.UI.Application.DTOS
{
    public class UpdateExternalUserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Code { get; set; }             
        public bool IsActive { get; set; }
        public UserLoginType UserLoginType { get; set; } 
        public List<string> Roles { get; set; }    
        public string? Password { get; set; }
    }
}
