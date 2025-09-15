using App.UI.Application.DTOS;

namespace App.UI.Presentation.ViewModels
{
    public class UserRoleAssignViewModel
    {
        public UserAppDtoUI User { get; set; }
        public List<RoleAssignDtoUI> UserRoles { get; set; } = new();
        public List<RoleDto> AllRoles { get; set; } = new();
    }
}
