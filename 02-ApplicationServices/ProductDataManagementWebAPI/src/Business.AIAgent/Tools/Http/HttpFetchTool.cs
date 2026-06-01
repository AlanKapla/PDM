using Business.AIAgent.Tools.Base;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Business.AIAgent.Tools.Http;

public sealed class HttpFetchTool : AgentToolBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpFetchTool(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override string Name => "http_fetch";

    public override string Description =>
        "Performs an HTTP GET or POST request to an external URL. Supports Bearer token from session context.";

    public override JsonElement ParametersSchema => BuildSchema("""
        {
          "type": "object",
          "properties": {
            "url": {
              "type": "string",
              "description": "Full URL to call"
            },
            "method": {
              "type": "string",
              "description": "HTTP method: GET or POST (default: GET)"
            },
            "body": {
              "type": "string",
              "description": "Optional JSON body for POST"
            },
            "use_auth": {
              "type": "boolean",
              "description": "If true, attaches Bearer token from session context"
            }
          },
          "required": ["url"]
        }
        """);

    public override async Task<ToolResult> ExecuteAsync(
        JsonElement arguments,
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        string? url = GetString(arguments, "url");
        if (string.IsNullOrWhiteSpace(url))
        {
            return ToolResult.Failure("url is required");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return ToolResult.Failure($"Invalid or non-HTTP URL: {url}");
        }

        string method = GetString(arguments, "method") ?? "GET";
        string? body = GetString(arguments, "body");
        bool useAuth = arguments.TryGetProperty("use_auth", out JsonElement useAuthEl) &&
                       useAuthEl.ValueKind == JsonValueKind.True;

        using HttpClient httpClient = _httpClientFactory.CreateClient("AIAgentHttp");

        if (useAuth && !string.IsNullOrWhiteSpace(context.BearerToken))
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", context.BearerToken);
        }

        HttpResponseMessage response;
        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(body))
        {
            using StringContent content = new(body, System.Text.Encoding.UTF8, "application/json");
            response = await httpClient.PostAsync(uri, content, cancellationToken);
        }
        else
        {
            response = await httpClient.GetAsync(uri, cancellationToken);
        }

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ToolResult.Failure($"HTTP {(int)response.StatusCode}: {responseBody[..Math.Min(500, responseBody.Length)]}");
        }

        return ToolResult.Success(responseBody.Length > 8000
            ? responseBody[..8000] + "... [truncated]"
            : responseBody);
    }
}
