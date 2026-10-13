using Desertod.Api.Auth;
using Desertod.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace Desertod.Api.Game;

/// <summary>
/// Turns game exceptions into problem details. The <c>code</c> and <c>arguments</c> extensions let the client
/// show the message in the player's language; <c>detail</c> is the English fallback.
/// </summary>
public sealed class GameExceptionHandler(IProblemDetailsService problemDetails, ILogger<GameExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, arguments) = exception switch
        {
            GameRuleException rule => (StatusCodes.Status400BadRequest, rule.Code, rule.Arguments),
            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Code, notFound.Arguments),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "access.denied", null),
            _ => (0, null, null),
        };

        if (status == 0)
        {
            return false;
        }

        // Runs outside the account log scope: the exception has already left it.
        logger.RequestRejected(http.Request.Method, http.Request.Path, http.User.GetAccountId(), status, code ?? "", exception.Message);
        http.Response.StatusCode = status;
        var context = new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = { Status = status, Detail = status == 403 ? "Not allowed." : exception.Message },
        };
        context.ProblemDetails.Extensions["code"] = code;
        context.ProblemDetails.Extensions["arguments"] = arguments ?? new Dictionary<string, object>();
        return await problemDetails.TryWriteAsync(context);
    }
}
