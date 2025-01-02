using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace UploadingLargeFiles.Utilities
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class MultipartFormDataAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;

            if (request.HasFormContentType && !string.IsNullOrEmpty(request.ContentType) && request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            context.Result = new StatusCodeResult(StatusCodes.Status415UnsupportedMediaType);
        }
    }
}