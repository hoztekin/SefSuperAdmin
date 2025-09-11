using Microsoft.AspNetCore.Mvc;

namespace App.UI.Components
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
