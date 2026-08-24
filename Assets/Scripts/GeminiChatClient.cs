using System;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public sealed class GeminiChatClient
{
    [SerializeField] private string apiKey = "";
    [SerializeField] private string model = "gemini-2.5-flash";
    [SerializeField] private int maxOutputTokens = 400;
    [SerializeField] private float temperature = 0.7f;

    public string ApiKey
    {
        get => apiKey;
        set => apiKey = value;
    }

    public string Model
    {
        get => string.IsNullOrWhiteSpace(model) ? "gemini-2.5-flash" : model;
        set => model = value;
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public async UniTask<string> GenerateResponseAsync(
        string prompt,
        string languageCode,
        string systemPrompt)
    {
        if (!HasApiKey)
            throw new InvalidOperationException("Gemini API key is missing.");

        if (string.IsNullOrWhiteSpace(prompt))
            return "";

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent";
        string requestJson = BuildRequestJson(prompt, languageCode, systemPrompt);
        byte[] body = Encoding.UTF8.GetBytes(requestJson);

        Debug.Log(
            $"[Gemini] Request start: model={Model}, " +
            $"language={languageCode}, promptLength={prompt.Length}");

        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-goog-api-key", ApiKey);

        await request.SendWebRequest().ToUniTask();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string message = string.IsNullOrWhiteSpace(request.downloadHandler.text)
                ? request.error
                : request.downloadHandler.text;
            Debug.LogError(
                $"[Gemini] Request failed: HTTP {request.responseCode}, " +
                $"{request.error}, body={Truncate(message, 240)}");
            throw new InvalidOperationException("Gemini request failed: " + message);
        }

        string text = ExtractText(request.downloadHandler.text);
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogError(
                $"[Gemini] Empty model response. body={Truncate(request.downloadHandler.text, 240)}");
        }
        return string.IsNullOrWhiteSpace(text)
            ? "I could not generate a response."
            : text.Trim();
    }

    private string BuildRequestJson(
        string prompt,
        string languageCode,
        string systemPrompt)
    {
        string languageName = languageCode == "de" ? "German" : "English";
        string system = string.IsNullOrWhiteSpace(systemPrompt)
            ? $"Answer in {languageName}. Give a complete medium-length reply by default, and follow any requested length or format."
            : systemPrompt + $" Answer in {languageName}.";

        return
            "{" +
            "\"systemInstruction\":{\"parts\":[{\"text\":\"" + Escape(system) + "\"}]}," +
            "\"contents\":[{\"role\":\"user\",\"parts\":[{\"text\":\"" + Escape(prompt) + "\"}]}]," +
            "\"generationConfig\":{" +
            "\"temperature\":" + temperature.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
            "\"maxOutputTokens\":" + maxOutputTokens +
            "}" +
            "}";
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var builder = new StringBuilder(value.Length + 16);
        foreach (char c in value)
        {
            switch (c)
            {
                case '\\': builder.Append("\\\\"); break;
                case '"': builder.Append("\\\""); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                default:
                    if (c < 32)
                        builder.Append("\\u").Append(((int)c).ToString("x4"));
                    else
                        builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }

    private static string ExtractText(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "";

        var matches = Regex.Matches(
            json,
            "\"text\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"",
            RegexOptions.CultureInvariant);

        if (matches.Count == 0)
            return "";

        var builder = new StringBuilder();
        for (int m = 0; m < matches.Count; m++)
        {
            string raw = matches[m].Groups[1].Value;
            string decoded = DecodeJsonString(raw);

            if (!string.IsNullOrWhiteSpace(decoded))
            {
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(decoded);
            }
        }

        return builder.ToString();
    }

    private static string DecodeJsonString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if (c != '\\' || i + 1 >= value.Length)
            {
                builder.Append(c);
                continue;
            }

            char next = value[++i];
            switch (next)
            {
                case '"': builder.Append('"'); break;
                case '\\': builder.Append('\\'); break;
                case '/': builder.Append('/'); break;
                case 'b': builder.Append('\b'); break;
                case 'f': builder.Append('\f'); break;
                case 'n': builder.Append('\n'); break;
                case 'r': builder.Append('\r'); break;
                case 't': builder.Append('\t'); break;
                case 'u':
                    if (i + 4 < value.Length &&
                        int.TryParse(value.Substring(i + 1, 4),
                            System.Globalization.NumberStyles.HexNumber,
                            null, out int code))
                    {
                        builder.Append((char)code);
                        i += 4;
                    }
                    break;
                default:
                    builder.Append(next);
                    break;
            }
        }

        return builder.ToString();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            return value ?? "";

        return value.Substring(0, maxLength);
    }
}
