using System.Text.Json;
using System.Text.Json.Serialization;

namespace App.Shared.Helpers
{
    /// <summary>
    /// Sadece tarih bilgisi için (doğum tarihi, son kullanma tarihi vb.)
    /// </summary>
    public class DateOnlyJsonConverter : JsonConverter<DateTime>
    {
        private const string DateFormat = "yyyy-MM-dd";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (DateTime.TryParseExact(dateString, DateFormat, null, System.Globalization.DateTimeStyles.None, out var date))
            {
                return date;
            }
            return DateTime.Parse(dateString!);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(DateFormat));
        }
    }

    /// <summary>
    /// Nullable tarih bilgisi için
    /// </summary>
    public class NullableDateOnlyJsonConverter : JsonConverter<DateTime?>
    {
        private const string DateFormat = "yyyy-MM-dd"; // ← Düzeltildi

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (string.IsNullOrEmpty(dateString))
                return null;

            if (DateTime.TryParseExact(dateString, DateFormat, null, System.Globalization.DateTimeStyles.None, out var date))
            {
                return date;
            }
            return DateTime.Parse(dateString);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString(DateFormat));
            else
                writer.WriteNullValue();
        }
    }

    /// <summary>
    /// UTC timestamp için (CreatedDate, UpdatedDate, audit alanları vb.)
    /// </summary>
    public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (DateTime.TryParseExact(dateString, DateTimeFormat, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var date))
            {
                return date;
            }
            return DateTime.Parse(dateString!).ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString(DateTimeFormat));
        }
    }

    /// <summary>
    /// Nullable UTC timestamp için
    /// </summary>
    public class NullableUtcDateTimeJsonConverter : JsonConverter<DateTime?>
    {
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (string.IsNullOrEmpty(dateString))
                return null;

            if (DateTime.TryParseExact(dateString, DateTimeFormat, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var date))
            {
                return date;
            }
            return DateTime.Parse(dateString).ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToUniversalTime().ToString(DateTimeFormat));
            else
                writer.WriteNullValue();
        }
    }
}
