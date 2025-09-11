using App.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MemberController(IMemberService memberService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var result = await memberService.GetAllMembersAsync();
            return View(result.ToList());
        }
    }
}
