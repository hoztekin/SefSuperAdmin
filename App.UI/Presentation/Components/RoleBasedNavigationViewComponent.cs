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
                ShowLogo = showLogo
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
                }
            }

            return View(viewModel);
        }
    }
}
