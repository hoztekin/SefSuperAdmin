using Microsoft.AspNetCore.Mvc;

namespace App.UI.Components
{
    public class FooterViewComponent : ViewComponent
    {
        public FooterViewComponent()
        {

        }
        public IViewComponentResult Invoke()
        {

            return View();
        }
    }
}
