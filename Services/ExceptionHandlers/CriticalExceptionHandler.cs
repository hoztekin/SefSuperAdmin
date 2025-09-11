using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace App.Services.ExceptionHandlers
{
    class CriticalExceptionHandler : IExceptionHandler
    {
        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is CriticalException)
            {
                Log.Error(exception, "Kritik bir hata oluştu");
            }

            return ValueTask.FromResult(false);
        }
    }
}
