using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuanLyCuaHangMyPham.Utilities
{
    public class DateTimeFormatConverter : JsonConverter<DateTime>
    {
        private readonly string _format;

        public DateTimeFormatConverter(string format)
        {
            _format = format;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Kiểm tra và xử lý nếu định dạng không đúng
            if (reader.TokenType == JsonTokenType.String)
            {
                // Đọc giá trị chuỗi
                string dateString = reader.GetString();

                if (DateTime.TryParseExact(dateString, _format, null, DateTimeStyles.None, out var result))
                {
                    return result;
                }

                throw new JsonException($"Unable to parse date '{dateString}' to format '{_format}'.");
            }

            throw new JsonException("Invalid token type, expected a string.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format));
        }
    }
}
