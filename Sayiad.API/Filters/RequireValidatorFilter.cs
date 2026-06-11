using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Sayiad.Api.Filters;

public class RequireValidatorFilter : IActionFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RequireValidatorFilter> _logger;

    public RequireValidatorFilter(IServiceProvider serviceProvider, ILogger<RequireValidatorFilter> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var (key, value) in context.ActionArguments)
        {
            if (value is null) continue;
            var type = value.GetType();
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(type);
            if (_serviceProvider.GetService(validatorType) is null)
            {
                _logger.LogWarning("Missing FluentValidation validator for {DtoType} in {Controller}.{Action}",
                    type.Name, context.Controller.GetType().Name, context.ActionDescriptor.DisplayName);
                context.ModelState.AddModelError(key, $"Validation is not configured for {type.Name}. Contact support.");
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
                return;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
