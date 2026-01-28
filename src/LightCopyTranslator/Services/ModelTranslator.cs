using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LightCopyTranslator.Services;

internal sealed class ModelTranslator : ITranslator
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    private static readonly Regex SingleWordRegex = new("^[A-Za-z][A-Za-z'-]*$", RegexOptions.Compiled);
    private readonly string _name;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly Uri _baseUri;

    public ModelTranslator(ModelConfig config)
    {
        _name = string.IsNullOrWhiteSpace(config.Name) ? "Model" : config.Name.Trim();
        _apiKey = string.IsNullOrWhiteSpace(config.ApiKey)
            ? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
            : config.ApiKey;

        _model = string.IsNullOrWhiteSpace(config.Model) ? "deepseek/deepseek-chat" : config.Model.Trim();
        _baseUri = NormalizeBaseUri(config.BaseUrl);
    }

    public string Name => _name;

    public async Task<string> TranslateAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return $"{_name} 未配置：请设置 OPENROUTER_API_KEY 或在设置中填写 apiKey";
        }

        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Add("HTTP-Referer", "https://localhost");
        request.Headers.Add("X-Title", "LightCopyTranslator");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("LightCopyTranslator/1.0");

        var payload = new
        {
            model = _model,
            temperature = 0.2,
            messages = BuildMessages(text)
        };

        request.Content = JsonContent.Create(payload);

        using var response = await Http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(errorBody))
            {
                var snippet = errorBody.Length > 300 ? errorBody[..300] + "..." : errorBody;
                return $"{_name} 请求失败: {(int)response.StatusCode}\n{snippet}";
            }

            return $"{_name} 请求失败: {(int)response.StatusCode}";
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"{_name} 返回为空";
        }

        if (!body.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            var snippet = body.Length > 300 ? body[..300] + "..." : body;
            return $"{_name} 返回非 JSON 响应（请检查 BaseUrl 是否为 https://openrouter.ai/api/v1 ）：\n{snippet}";
        }

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("choices", out var choices)
            && choices.ValueKind == JsonValueKind.Array
            && choices.GetArrayLength() > 0)
        {
            var message = choices[0].GetProperty("message");
            if (message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? "";
            }
        }

        return $"{_name} 返回内容解析失败";
    }

    private static object[] BuildMessages(string text)
    {
        var trimmed = text.Trim();
        var sysPrompt = SingleWordRegex.IsMatch(trimmed)
            ? "你是英译中词典。用户输入一个英文单词时，只输出中文释义。给出常见词性与简明释义，多词性分行，例如：\n" +
              "n. 释义\nv. 释义\nadj. 释义\n不要给例句、不要解释、不要多余文本。"
            : "你是英译中翻译器。用户输入英文句子或段落时，只输出流畅准确的中文翻译，不要解释、不要附加内容。";

        return
        [
            new { role = "system", content = sysPrompt },
            new { role = "user", content = trimmed }
        ];
    }

    private static Uri NormalizeBaseUri(string? baseUrl)
    {
        var value = string.IsNullOrWhiteSpace(baseUrl) ? "https://openrouter.ai/api/v1" : baseUrl.Trim();
        value = value.TrimEnd('/');

        if (!value.Contains("/api/", StringComparison.OrdinalIgnoreCase))
        {
            value = $"{value}/api/v1";
        }

        return new Uri(value + "/");
    }
}
