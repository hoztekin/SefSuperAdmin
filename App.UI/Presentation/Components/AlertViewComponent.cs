using Microsoft.AspNetCore.Mvc;

namespace App.UI.Presentation.Components
{
    public class AlertViewComponent : ViewComponent
    {
        public AlertViewComponent()
        {

        }
        public IViewComponentResult Invoke()
        {

            return View();
        }
    }
}
