using Microsoft.AspNetCore.Mvc;

namespace App.UI.Helper
{
    public static class AlertHelper
    {
        public static void SetSuccessMessage(this Controller controller, string message)
        {
            controller.TempData["SuccessMessage"] = message;
        }

        public static void SetErrorMessage(this Controller controller, string message)
        {
            controller.TempData["ErrorMessage"] = message;
        }

        public static void SetWarningMessage(this Controller controller, string message)
        {
            controller.TempData["WarningMessage"] = message;
        }

        public static void SetInfoMessage(this Controller controller, string message)
        {
            controller.TempData["InfoMessage"] = message;
        }
    }
}
