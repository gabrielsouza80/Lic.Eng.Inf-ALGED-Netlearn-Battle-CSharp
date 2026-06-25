using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace NetLearnBattle.CSharp.Models;

// Algumas regras ACL usam 80 e outras usam "any". Ambos são válidos.
public class FlexibleStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.Number => reader.GetInt32().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Null => string.Empty,
            _ => throw new JsonException("A porta ACL deve ser texto ou número."),
        };
    }

    public override void Write(Utf8JsonWriter writer, string value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
