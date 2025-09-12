using Microsoft.AspNetCore.Mvc;

namespace App.UI.Components
{
    public class SideBarViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var sidebarModel = new SidebarViewModel
            {
                CurrentController = ViewContext.RouteData.Values["Controller"]?.ToString(),
                CurrentAction = ViewContext.RouteData.Values["Action"]?.ToString(),
                UserName = "Administrator",
                UserRole = "Sistem Yöneticisi"
            };

            return View(sidebarModel);
        }
    }

    public class SidebarViewModel
    {
        public string CurrentController { get; set; }
        public string CurrentAction { get; set; }
        public string UserName { get; set; }
        public string UserRole { get; set; }

        public bool IsActive(string controller, string action = null)
        {
            if (action == null)
                return CurrentController?.Equals(controller, StringComparison.OrdinalIgnoreCase) == true;

            return CurrentController?.Equals(controller, StringComparison.OrdinalIgnoreCase) == true &&
                   CurrentAction?.Equals(action, StringComparison.OrdinalIgnoreCase) == true;
        }

        public bool IsParentActive(params string[] controllers)
        {
            return controllers.Any(controller =>
                CurrentController?.Equals(controller, StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}
