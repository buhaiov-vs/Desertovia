namespace Desertod.Api;

/// <summary>
/// Command-line probe for the container health check: the runtime image has no curl or wget.
/// </summary>
public static class HealthProbe
{
    public const string Argument = "--health-check";
    public const string Path = "/healthz";

    public static async Task<int> RunAsync()
    {
        var port = (Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080")
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "8080";

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            using var response = await client.GetAsync($"http://localhost:{port}{Path}");
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            await Console.Error.WriteLineAsync(exception.Message);
            return 1;
        }
    }
}
