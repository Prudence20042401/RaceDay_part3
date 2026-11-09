using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.MVC.Services;

/// <summary>Server-side client for the Part 2 API. API authentication cookies are kept in MVC session.</summary>
public sealed class RaceDayApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<RaceDayApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    private ISession? Session => _accessor.HttpContext?.Session;

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null, IFormFile? file = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, path);
        var cookies = Session?.GetString("ApiCookies");
        if (!string.IsNullOrWhiteSpace(cookies)) request.Headers.TryAddWithoutValidation("Cookie", cookies);

        if (file != null)
        {
            var form = new MultipartFormDataContent();
            var stream = file.OpenReadStream();
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(content, "file", Path.GetFileName(file.FileName));
            request.Content = form;
        }
        else if (body != null) request.Content = JsonContent.Create(body);

        try
        {
            var response = await _http.SendAsync(request, cancellationToken);
            SaveCookies(response);
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not reach the RaceDay API at {BaseAddress}", _http.BaseAddress);
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Content = JsonContent.Create(new { message = "The RaceDay API is currently unavailable. Please try again shortly." })
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Content = JsonContent.Create(new { message = "The RaceDay API request timed out. Please try again." })
            };
        }
    }

    private void SaveCookies(HttpResponseMessage response)
    {
        if (Session == null || !response.Headers.TryGetValues("Set-Cookie", out var values)) return;
        var current = (Session.GetString("ApiCookies") ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Contains('=')).ToDictionary(x => x[..x.IndexOf('=')], x => x[(x.IndexOf('=') + 1)..], StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            var pair = value.Split(';', 2)[0];
            var index = pair.IndexOf('=');
            if (index > 0) current[pair[..index]] = pair[(index + 1)..];
        }
        Session.SetString("ApiCookies", string.Join("; ", current.Select(x => $"{x.Key}={x.Value}")));
    }

    public async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task<string> ErrorAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("message", out var message)) return message.GetString() ?? "The request could not be completed.";
            if (doc.RootElement.TryGetProperty("title", out var title)) return title.GetString() ?? "Please check your information.";
        }
        catch { }
        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            System.Net.HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
            System.Net.HttpStatusCode.NotFound => "The requested item could not be found.",
            System.Net.HttpStatusCode.Conflict => "This action conflicts with existing records.",
            _ => "Something went wrong. Please check your details and try again."
        };
    }
}
