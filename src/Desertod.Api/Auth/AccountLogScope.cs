namespace Desertod.Api.Auth;

public static class AccountLogScope
{
    /// <summary>Tags every log written during a signed-in request with the account id. Must run after authentication.</summary>
    public static IApplicationBuilder UseAccountLogScope(this IApplicationBuilder app)
    {
        var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AccountLogScope));
        return app.Use(async (http, next) =>
        {
            if (http.User.GetAccountId() is not { } accountId)
            {
                await next(http);
                return;
            }

            using (logger.BeginScope("Account {AccountId}", accountId))
            {
                await next(http);
            }
        });
    }
}
