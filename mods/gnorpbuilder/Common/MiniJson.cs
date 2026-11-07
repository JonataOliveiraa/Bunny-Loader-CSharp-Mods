using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GnorpBuilderMod.Common;

// Reads Builds/index.json. The phone has no System.Text.Json.
// Objects come back as dictionaries, arrays as lists, numbers as doubles.
public sealed class MiniJson
{
    readonly string text;
    int pos;

    MiniJson(string text) => this.text = text;

    public static object? Parse(string text)
    {
        var reader = new MiniJson(text);
        object? value = reader.ReadValue();
        reader.SkipSpace();
        if (reader.pos != text.Length) throw reader.Fail("trailing text");
        return value;
    }

    object? ReadValue()
    {
        SkipSpace();
        if (pos >= text.Length) throw Fail("unexpected end");
        switch (text[pos])
        {
            case '{': return ReadObject();
            case '[': return ReadArray();
            case '"': return ReadString();
            case 't': return ReadWord("true", true);
            case 'f': return ReadWord("false", false);
            case 'n': return ReadWord("null", null);
            default: return ReadNumber();
        }
    }

    Dictionary<string, object?> ReadObject()
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        pos++;
        SkipSpace();
        if (Peek() == '}') { pos++; return result; }
        while (true)
        {
            SkipSpace();
            string key = ReadString();
            SkipSpace();
            if (Peek() != ':') throw Fail("expected ':'");
            pos++;
            result[key] = ReadValue();
            SkipSpace();
            char next = Peek();
            pos++;
            if (next == '}') return result;
            if (next != ',') throw Fail("expected ',' or '}'");
        }
    }

    List<object?> ReadArray()
    {
        var result = new List<object?>();
        pos++;
        SkipSpace();
        if (Peek() == ']') { pos++; return result; }
        while (true)
        {
            result.Add(ReadValue());
            SkipSpace();
            char next = Peek();
            pos++;
            if (next == ']') return result;
            if (next != ',') throw Fail("expected ',' or ']'");
        }
    }

    string ReadString()
    {
        if (Peek() != '"') throw Fail("expected a string");
        pos++;
        var result = new StringBuilder();
        while (true)
        {
            char c = Peek();
            pos++;
            if (c == '"') return result.ToString();
            if (c != '\\') { result.Append(c); continue; }
            char escape = Peek();
            pos++;
            switch (escape)
            {
                case 'n': result.Append('\n'); break;
                case 't': result.Append('\t'); break;
                case 'r': result.Append('\r'); break;
                case 'b': result.Append('\b'); break;
                case 'f': result.Append('\f'); break;
                case 'u':
                    if (pos + 4 > text.Length) throw Fail("bad escape");
                    result.Append((char)int.Parse(text.AsSpan(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    pos += 4;
                    break;
                default: result.Append(escape); break;
            }
        }
    }

    object? ReadWord(string word, object? value)
    {
        if (string.CompareOrdinal(text, pos, word, 0, word.Length) != 0) throw Fail("unknown word");
        pos += word.Length;
        return value;
    }

    double ReadNumber()
    {
        int start = pos;
        while (pos < text.Length && "+-.eE0123456789".Contains(text[pos])) pos++;
        if (start == pos) throw Fail("unexpected character");
        return double.Parse(text.AsSpan(start, pos - start), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    char Peek()
    {
        if (pos >= text.Length) throw Fail("unexpected end");
        return text[pos];
    }

    void SkipSpace()
    {
        while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
    }

    FormatException Fail(string message) => new("index.json, position " + pos + ": " + message);
}
