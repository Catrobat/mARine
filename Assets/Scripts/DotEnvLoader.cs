using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DotEnvLoader
{
    private static Dictionary<string, string> _values;

    public static string Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "";

        EnsureLoaded();
        return _values != null && _values.TryGetValue(key, out string value)
            ? value
            : "";
    }

    private static void EnsureLoaded()
    {
        if (_values != null)
            return;

        _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string content = LoadEnvContent();
        if (string.IsNullOrWhiteSpace(content))
            return;

        foreach (string rawLine in content.Split(
            new[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None))
        {
            AddLine(rawLine);
        }
    }

    private static string LoadEnvContent()
    {
#if UNITY_EDITOR
        string envPath = GetEditorEnvPath();
        return !string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath)
            ? File.ReadAllText(envPath)
            : "";
#else
        TextAsset envAsset = Resources.Load<TextAsset>("BuildEnv");
        if (envAsset != null)
            return envAsset.text;

        string envPath = Path.Combine(Application.streamingAssetsPath, ".env");
        return File.Exists(envPath) ? File.ReadAllText(envPath) : "";
#endif
    }

    private static void AddLine(string rawLine)
    {
        string line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            return;

        int equalsIndex = line.IndexOf('=');
        if (equalsIndex <= 0)
            return;

        string key = line.Substring(0, equalsIndex).Trim();
        string value = line.Substring(equalsIndex + 1).Trim();

        if (value.Length >= 2 &&
            ((value[0] == '"' && value[value.Length - 1] == '"') ||
             (value[0] == '\'' && value[value.Length - 1] == '\'')))
        {
            value = value.Substring(1, value.Length - 2);
        }

        if (!string.IsNullOrWhiteSpace(key))
            _values[key] = value;
    }

    private static string GetEditorEnvPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (!string.IsNullOrWhiteSpace(projectRoot))
            return Path.Combine(projectRoot, ".env");

        return "";
    }
}
