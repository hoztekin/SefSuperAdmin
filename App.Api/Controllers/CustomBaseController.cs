using App.Services;
using App.Shared.RequestFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace App.Api.Controllers
{
    //[Route("api/[controller]")]    
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomBaseController : ControllerBase
    {

        protected string ApiVersion
        {
            get
            {
                var controllerPath = ControllerContext.ActionDescriptor.ControllerTypeInfo.CustomAttributes
                    .FirstOrDefault(a => a.AttributeType == typeof(RouteAttribute))
                    ?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";

                if (controllerPath.Contains("v1"))
                    return "v1";
                else if (controllerPath.Contains("v2"))
                    return "v2";

                return "v1";
            }
        }

        private void AddApiVersionHeader()
        {
            if (!Response.Headers.ContainsKey("X-API-Version"))
                Response.Headers.TryAdd("X-API-Version", ApiVersion);
        }

        [NonAction]
        protected IActionResult CreateActionResult<T>(ServiceResult<T> result)
        {
            AddApiVersionHeader();

            if (result.Status == HttpStatusCode.NoContent)
                return NoContent();


            if (result.Status == HttpStatusCode.Created)
                return Created(result.UrlAsCreated, result);

            return new ObjectResult(result) { StatusCode = result.Status.GetHashCode() };
        }

        [NonAction]
        protected IActionResult CreateActionResult(ServiceResult result)
        {
            AddApiVersionHeader();

            if (result.Status == HttpStatusCode.NoContent)
                return NoContent();

            return new ObjectResult(result) { StatusCode = result.Status.GetHashCode() };
        }

        [NonAction]
        protected IActionResult CreatePaginatedActionResult<T>(ServiceResult<PagedList<T>> result)
        {
            AddApiVersionHeader();

            if (result.IsSuccess)
                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(result.Data.MetaData));

            return CreateActionResult(result);
        }
    }
}
