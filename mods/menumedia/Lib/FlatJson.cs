using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MenuMediaPort.Lib;

// Reads the top level of a JSON object, for config.json: the phone has no System.Text.Json.
// Values are string, double or bool; nested values and null are checked and left out.
public static class FlatJson
{
    public static Dictionary<string, object> Parse(string text)
    {
        var result = new Dictionary<string, object>();
        int at = 0;
        Skip(text, ref at);
        Expect(text, ref at, '{');
        Skip(text, ref at);
        if (Peek(text, at) == '}')
        {
            at++;
        }
        else
        {
            while (true)
            {
                Skip(text, ref at);
                string key = ReadString(text, ref at);
                Skip(text, ref at);
                Expect(text, ref at, ':');
                object? value = ReadValue(text, ref at);
                if (value != null)
                    result[key] = value;
                Skip(text, ref at);
                char next = Peek(text, at);
                at++;
                if (next == '}')
                    break;
                if (next != ',')
                    throw new FormatException("expected , or } at " + (at - 1));
            }
        }
        Skip(text, ref at);
        if (at < text.Length)
            throw new FormatException("text after the object at " + at);
        return result;
    }

    static char Peek(string text, int at) => at < text.Length ? text[at] : '\0';

    static void Skip(string text, ref int at)
    {
        // A file saved with a byte order mark starts with one.
        while (at < text.Length && (char.IsWhiteSpace(text[at]) || text[at] == '﻿'))
            at++;
    }

    static void Expect(string text, ref int at, char c)
    {
        if (Peek(text, at) != c)
            throw new FormatException("expected " + c + " at " + at);
        at++;
    }

    static string ReadString(string text, ref int at)
    {
        Expect(text, ref at, '"');
        var builder = new StringBuilder();
        while (true)
        {
            if (at >= text.Length)
                throw new FormatException("unterminated string");
            char c = text[at++];
            if (c == '"')
                return builder.ToString();
            if (c != '\\')
            {
                builder.Append(c);
                continue;
            }
            char escaped = Peek(text, at);
            at++;
            switch (escaped)
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
                    if (at + 4 > text.Length || !int.TryParse(text.AsSpan(at, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                        throw new FormatException("bad escape at " + at);
                    builder.Append((char)code);
                    at += 4;
                    break;
                default:
                    throw new FormatException("bad escape at " + at);
            }
        }
    }

    static object? ReadValue(string text, ref int at)
    {
        Skip(text, ref at);
        char c = Peek(text, at);
        if (c == '"')
            return ReadString(text, ref at);
        if (c == '{' || c == '[')
        {
            SkipNested(text, ref at);
            return null;
        }
        int start = at;
        while (at < text.Length && text[at] != ',' && text[at] != '}' && text[at] != ']' && !char.IsWhiteSpace(text[at]))
            at++;
        string word = text[start..at];
        switch (word)
        {
            case "true": return true;
            case "false": return false;
            case "null": return null;
        }
        if (double.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            return number;
        throw new FormatException("bad value at " + start);
    }

    static void SkipNested(string text, ref int at)
    {
        char open = text[at];
        at++;
        Skip(text, ref at);
        char close = open == '{' ? '}' : ']';
        if (Peek(text, at) == close)
        {
            at++;
            return;
        }
        while (true)
        {
            if (open == '{')
            {
                Skip(text, ref at);
                ReadString(text, ref at);
                Skip(text, ref at);
                Expect(text, ref at, ':');
            }
            ReadValue(text, ref at);
            Skip(text, ref at);
            char next = Peek(text, at);
            at++;
            if (next == close)
                return;
            if (next != ',')
                throw new FormatException("expected , at " + (at - 1));
        }
    }
}
