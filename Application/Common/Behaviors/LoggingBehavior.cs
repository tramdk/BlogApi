using Microsoft.Extensions.Logging;
using MediatR;

namespace FloraCore.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {Name}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {Name}", typeof(TRequest).Name);
        return response;
    }
}
