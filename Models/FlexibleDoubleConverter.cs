using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetLearnBattle.CSharp.Models;

// [M08][M11] Aceita dados antigos onde o tempo possa estar vazio ou ser nulo.
public class FlexibleDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetDouble();

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var time) ? time : 0;
        }

        return 0;
    }

    public override void Write(Utf8JsonWriter writer, double value,
        JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
