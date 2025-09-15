using App.UI.Infrastructure.Storage;
using App.UI.Presentation.ViewModels;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace App.UI.Presentation.Components
{
    public class RoleBasedNavigationViewComponent : ViewComponent
    {
        private readonly ISessionService _sessionService;

        public RoleBasedNavigationViewComponent(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string cssClass = "", bool showLogo = true)
        {
            var viewModel = new RoleBasedNavigationViewModel
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                UserName = User.Identity?.Name ?? "",
                IsAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin"),
                IsSuperAdmin = User.IsInRole("SuperAdmin"),
                CssClass = cssClass,
                ShowLogo = showLogo,
                UserRoles = GetUserRoles()
            };

            // Seçili makine bilgisi varsa ekle
            if (viewModel.IsAuthenticated && !viewModel.IsAdmin)
            {
                try
                {
                    var selectedMachine = _sessionService.GetSelectedMachine();
                    viewModel.HasSelectedMachine = selectedMachine != null;
                    viewModel.SelectedMachineName = selectedMachine?.BranchName ?? "";
                }
                catch
                {
                    viewModel.HasSelectedMachine = false;
                    viewModel.SelectedMachineName = "";
                }
            }

            return View(viewModel);
        }

        private List<string> GetUserRoles()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return new List<string>();

            // User'ı ClaimsPrincipal'a cast et
            if (User is ClaimsPrincipal claimsPrincipal)
            {
                return claimsPrincipal.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();
            }

            return new List<string>();
        }
    }
}
