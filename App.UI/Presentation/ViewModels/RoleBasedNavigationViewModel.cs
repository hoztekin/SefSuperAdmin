namespace App.UI.Presentation.ViewModels
{
    public class RoleBasedNavigationViewModel
    {
        public bool IsAuthenticated { get; set; }
        public string UserName { get; set; } = "";
        public bool IsAdmin { get; set; }
        public bool IsSuperAdmin { get; set; }
        public List<string> UserRoles { get; set; } = new();
        public bool HasSelectedMachine { get; set; }
        public string SelectedMachineName { get; set; } = "";
        public string CssClass { get; set; } = "";
        public bool ShowLogo { get; set; } = true;
    }
}
