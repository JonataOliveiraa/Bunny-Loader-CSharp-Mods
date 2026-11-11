using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MenuMediaPort.Lib;

// Reads the top level of a JSON object, for config.json.
// Values are string, double or bool; nested values and null are left out.
public static class FlatJson
{
    public static Dictionary<string, object> Parse(string text)
    {
        var result = new Dictionary<string, object>();
        // A file saved with a byte order mark starts with one.
        using JsonDocument json = JsonDocument.Parse(text.TrimStart('\uFEFF'));
        if (json.RootElement.ValueKind != JsonValueKind.Object)
            throw new FormatException("expected an object");
        foreach (JsonProperty property in json.RootElement.EnumerateObject())
        {
            JsonElement value = property.Value;
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    result[property.Name] = value.GetString()!;
                    break;
                case JsonValueKind.Number:
                    result[property.Name] = value.GetDouble();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    result[property.Name] = value.GetBoolean();
                    break;
            }
        }
        return result;
    }
}
