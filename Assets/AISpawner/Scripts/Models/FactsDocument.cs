using System.Collections.Generic;
using System.Text;

namespace MarineAR.AISpawner.Models
{
    /// <summary>
    /// Tolerant reader for the per-organism facts JSON files.
    /// The facts schema is intentionally open: every organism carries a flat JSON object
    /// whose keys vary by species (e.g. <c>lifespan</c> only when known). This parser
    /// reads any flat object of string / number / bool / null values, preserves key
    /// order, and skips nested structures instead of failing — so the dataset can keep
    /// evolving without app updates.
    /// </summary>
    public sealed class FactsDocument
    {
        readonly List<KeyValuePair<string, string>> m_Entries = new List<KeyValuePair<string, string>>();

        /// <summary>Ordered key/value facts. Values are display-ready strings.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Entries => m_Entries;

        public int Count => m_Entries.Count;

        public bool TryGet(string key, out string value)
        {
            foreach (var entry in m_Entries)
            {
                if (entry.Key == key)
                {
                    value = entry.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Parses a flat JSON object. Returns an empty document (never null) on malformed input.
        /// Null values are omitted — an absent fact should simply not be displayed.
        /// </summary>
        public static FactsDocument Parse(string json)
        {
            var doc = new FactsDocument();
            if (string.IsNullOrWhiteSpace(json))
                return doc;

            int i = 0;
            SkipWhitespace(json, ref i);
            if (i >= json.Length || json[i] != '{')
                return doc;

            i++; // consume '{'
            while (i < json.Length)
            {
                SkipWhitespace(json, ref i);
                if (i >= json.Length || json[i] == '}')
                    break;

                if (json[i] != '"')
                    break; // malformed — stop rather than throw

                string key = ReadString(json, ref i);
                SkipWhitespace(json, ref i);
                if (i >= json.Length || json[i] != ':')
                    break;
                i++; // consume ':'
                SkipWhitespace(json, ref i);

                if (i >= json.Length)
                    break;

                char c = json[i];
                if (c == '"')
                {
                    string value = ReadString(json, ref i);
                    if (!string.IsNullOrEmpty(value))
                        doc.m_Entries.Add(new KeyValuePair<string, string>(key, value));
                }
                else if (c == '{' || c == '[')
                {
                    SkipStructure(json, ref i); // nested — tolerated, not displayed
                }
                else
                {
                    string literal = ReadLiteral(json, ref i);
                    if (literal != "null" && literal.Length > 0)
                        doc.m_Entries.Add(new KeyValuePair<string, string>(key, literal));
                }

                SkipWhitespace(json, ref i);
                if (i < json.Length && json[i] == ',')
                    i++;
            }

            return doc;
        }

        /// <summary>Converts a snake_case JSON key into a display label ("scientific_name" → "Scientific Name").</summary>
        public static string PrettifyKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            var sb = new StringBuilder(key.Length + 4);
            bool startOfWord = true;
            foreach (char c in key)
            {
                if (c == '_' || c == '-')
                {
                    sb.Append(' ');
                    startOfWord = true;
                }
                else
                {
                    sb.Append(startOfWord ? char.ToUpperInvariant(c) : c);
                    startOfWord = false;
                }
            }

            return sb.ToString();
        }

        static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
        }

        static string ReadString(string s, ref int i)
        {
            // Precondition: s[i] == '"'
            i++;
            var sb = new StringBuilder();
            while (i < s.Length && s[i] != '"')
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    i++;
                    char esc = s[i];
                    switch (esc)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': break;
                        case 'u':
                            if (i + 4 < s.Length &&
                                int.TryParse(s.Substring(i + 1, 4),
                                    System.Globalization.NumberStyles.HexNumber,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out int code))
                            {
                                sb.Append((char)code);
                                i += 4;
                            }
                            break;
                        default: sb.Append(esc); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
                i++;
            }

            if (i < s.Length)
                i++; // consume closing '"'
            return sb.ToString();
        }

        static string ReadLiteral(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && s[i] != ',' && s[i] != '}' && !char.IsWhiteSpace(s[i]))
                i++;
            return s.Substring(start, i - start);
        }

        static void SkipStructure(string s, ref int i)
        {
            char open = s[i];
            char close = open == '{' ? '}' : ']';
            int depth = 0;
            bool inString = false;

            for (; i < s.Length; i++)
            {
                char c = s[i];
                if (inString)
                {
                    if (c == '\\')
                        i++;
                    else if (c == '"')
                        inString = false;
                    continue;
                }

                if (c == '"')
                    inString = true;
                else if (c == open)
                    depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        i++;
                        return;
                    }
                }
            }
        }
    }
}
