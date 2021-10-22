using System;
using System.Text.Json;

namespace AlarmConnect.Models
{
    public static class DataObjectExtensions
    {
        /// <summary>
        /// Get an attribute value as a specific object.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <param name="defaultValue">The default value if the attribute is not set.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Returns the attribute value or the default value if not set.</returns>
        /// <exception cref="InvalidCastException"></exception>
        /// <exception cref="JsonException"></exception>
        public static T GetAttributeAs<T>(this IDataObject self, string name, T defaultValue = null) where T : class, new()
        {
            var val = self.GetAttribute(name);
            if (val is null) return defaultValue;
            if (val is T tVal) return tVal;

            var t = typeof(T);
            if (t == typeof(string))
            {
                val = val.ToString();
                return (T)val;
            }

            if (val is JsonElement json)
            {
                if ((t.IsArray && json.ValueKind == JsonValueKind.Array) ||
                    (!t.IsArray && json.ValueKind == JsonValueKind.Object))
                {
                    var rawJson = json.GetRawText();
                    return JsonSerializer.Deserialize<T>(rawJson);
                }
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Get a boolean attribute.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool GetBooleanAttribute(this IDataObject self, string name, bool defaultValue = false)
        {
            var value = self.GetAttribute(name);
            return value is bool b ? b : bool.TryParse(value?.ToString(), out var b2) ? b2 : defaultValue;
        }

        /// <summary>
        /// Get a string attribute.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetStringAttribute(this IDataObject self, string name, string defaultValue = "")
            => self.GetAttribute(name)?.ToString() ?? defaultValue;

        /// <summary>
        /// Get an integer attribute.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int GetInt32Attribute(this IDataObject self, string name, int defaultValue = 0)
        {
            var value = self.GetAttribute(name);
            return value is int i ? i : int.TryParse(value?.ToString(), out var i2) ? i2 : defaultValue;
        }

        /// <summary>
        /// Get an integer attribute.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static long GetInt64Attribute(this IDataObject self, string name, long defaultValue = 0)
        {
            var value = self.GetAttribute(name);
            return value is long i ? i : long.TryParse(value?.ToString(), out var i2) ? i2 : defaultValue;
        }

        /// <summary>
        /// Get a floating point attribute.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double GetDoubleAttribute(this IDataObject self, string name, double defaultValue = 0)
        {
            var value = self.GetAttribute(name);
            return value is double d ? d : long.TryParse(value?.ToString(), out var d2) ? d2 : defaultValue;
        }
    }
}
