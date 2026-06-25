using Microsoft.AspNetCore.WebUtilities;

namespace TravelOptimizer.Api.Common;

/// <summary>
/// Optionally authenticates the TfL agents' HTTP calls. If a key is configured (Travel:Tfl:AppKey,
/// falling back to the TFL_APP_KEY env var) it is appended as the <c>app_key</c> query parameter per
/// TfL convention; absent a key the handler is a no-op and the agents run unauthenticated.
/// </summary>
public class TflAppKeyHandler : DelegatingHandler
{
    private readonly string? _appKey;

    public TflAppKeyHandler(IConfiguration config)
    {
        _appKey = config["Travel:Tfl:AppKey"];
        if (string.IsNullOrWhiteSpace(_appKey))
            _appKey = Environment.GetEnvironmentVariable("TFL_APP_KEY");
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_appKey) && request.RequestUri is not null)
        {
            var withKey = QueryHelpers.AddQueryString(request.RequestUri.ToString(), "app_key", _appKey);
            request.RequestUri = new Uri(withKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
