using Business.Interfaces.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace WebApi.Controllers;

/// <summary>
/// Reverse proxy for Entra External ID Native Auth API.
/// Native Auth has no CORS — SPA must call this API proxy (or a same-origin gateway).
/// Azure Static Web Apps returns 405 for POST /native-auth on the static host.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/native-auth")]
[DisableFormValueModelBinding]
public sealed class NativeAuthProxyController : ControllerBase
{
    private static readonly HashSet<string> AllowedRequestHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Content-Type",
        "Accept",
        "Client-Request-Id",
        "x-client-sku",
        "x-client-ver",
        "x-client-os",
        "x-client-cpu",
        "x-client-current-telemetry",
        "x-client-last-telemetry",
    };

    private static readonly HashSet<string> BlockedResponseHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "transfer-encoding",
        "connection",
        "keep-alive",
        "content-length",
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AzureAdB2CSettings _azureAdB2CSettings;
    private readonly ILogger<NativeAuthProxyController> _logger;

    public NativeAuthProxyController(
        IHttpClientFactory httpClientFactory,
        IOptions<AzureAdB2CSettings> azureAdB2COptions,
        ILogger<NativeAuthProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _azureAdB2CSettings = azureAdB2COptions.Value;
        _logger = logger;
    }

    [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
    [Route("{**path}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task ProxyAsync(string? path, CancellationToken cancellationToken)
    {
        string relativePath = (path ?? string.Empty).TrimStart('/');
        if (!IsAllowedNativeAuthPath(relativePath))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Title = "Path not allowed",
                    Detail = "Only Entra Native Auth API paths may be proxied.",
                    Status = StatusCodes.Status400BadRequest
                },
                cancellationToken);
            return;
        }

        string targetBase = BuildNativeAuthBaseUrl();
        string query = Request.QueryString.HasValue ? Request.QueryString.Value! : string.Empty;
        Uri targetUri = new($"{targetBase}/{relativePath}{query}");

        using HttpRequestMessage upstreamRequest = new(new HttpMethod(Request.Method), targetUri);

        if (HttpMethods.IsPost(Request.Method)
            || HttpMethods.IsPut(Request.Method)
            || HttpMethods.IsPatch(Request.Method))
        {
            byte[] bodyBytes = await ReadRequestBodyAsync(cancellationToken);
            if (bodyBytes.Length == 0)
            {
                _logger.LogWarning(
                    "Native Auth proxy received empty body for {Method} {Path}",
                    Request.Method,
                    relativePath);
            }
            else if (_logger.IsEnabled(LogLevel.Debug))
            {
                bool hasClientId = Encoding.UTF8.GetString(bodyBytes).Contains("client_id=", StringComparison.Ordinal);
                _logger.LogDebug(
                    "Native Auth proxy forwarding {Method} {Path} ({Length} bytes, hasClientId={HasClientId})",
                    Request.Method,
                    relativePath,
                    bodyBytes.Length,
                    hasClientId);
            }

            // ByteArrayContent sets Content-Length — Entra rejects chunked/empty form bodies (AADSTS900144).
            ByteArrayContent content = new(bodyBytes);
            string contentType = string.IsNullOrWhiteSpace(Request.ContentType)
                ? "application/x-www-form-urlencoded"
                : Request.ContentType;
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            upstreamRequest.Content = content;
        }

        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in Request.Headers)
        {
            if (!AllowedRequestHeaders.Contains(header.Key))
            {
                continue;
            }

            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!upstreamRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                upstreamRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        HttpClient client = _httpClientFactory.CreateClient("NativeAuthProxy");

        HttpResponseMessage upstreamResponse;
        try
        {
            upstreamResponse = await client.SendAsync(
                upstreamRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Native Auth proxy failed for {Path}", relativePath);
            Response.StatusCode = StatusCodes.Status502BadGateway;
            await Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Title = "Upstream Native Auth unavailable",
                    Status = StatusCodes.Status502BadGateway
                },
                cancellationToken);
            return;
        }

        using (upstreamResponse)
        {
            Response.StatusCode = (int)upstreamResponse.StatusCode;

            foreach (KeyValuePair<string, IEnumerable<string>> header in upstreamResponse.Headers)
            {
                if (BlockedResponseHeaders.Contains(header.Key))
                {
                    continue;
                }

                Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in upstreamResponse.Content.Headers)
            {
                if (BlockedResponseHeaders.Contains(header.Key))
                {
                    continue;
                }

                Response.Headers[header.Key] = header.Value.ToArray();
            }

            await upstreamResponse.Content.CopyToAsync(Response.Body, cancellationToken);
        }
    }

    private async Task<byte[]> ReadRequestBodyAsync(CancellationToken cancellationToken)
    {
        if (Request.Body.CanSeek)
        {
            Request.Body.Position = 0;
        }

        using MemoryStream buffer = new();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private string BuildNativeAuthBaseUrl()
    {
        string instance = _azureAdB2CSettings.Instance.TrimEnd('/');
        string domain = _azureAdB2CSettings.Domain.Trim().Trim('/');
        return $"{instance}/{domain}";
    }

    private static bool IsAllowedNativeAuthPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        if (relativePath.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        return relativePath.StartsWith("oauth2/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("signup/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("resetpassword/", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Prevents ASP.NET from consuming application/x-www-form-urlencoded before the proxy can forward it.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
file sealed class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        IList<IValueProviderFactory> factories = context.ValueProviderFactories;
        factories.RemoveType<FormValueProviderFactory>();
        factories.RemoveType<FormFileValueProviderFactory>();
        factories.RemoveType<JQueryFormValueProviderFactory>();
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
