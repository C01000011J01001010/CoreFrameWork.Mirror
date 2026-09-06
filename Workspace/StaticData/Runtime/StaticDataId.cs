using System;
namespace CoreEngine.StaticData
{
    public static class StaticDataId
    {
        public const int MinValue = 1;
        public const int MaxValue = 999_999_999;
        public const int FileNameWidth = 9;
        public static bool IsValid(int id) => id >= MinValue && id <= MaxValue;
        public static void EnsureValid(int id, string parameterName = "id")
        {
            if (!IsValid(id)) throw new ArgumentOutOfRangeException(parameterName, id, $"Static-data IDs must be between {MinValue:N0} and {MaxValue:N0}.");
        }
        public static string ToFileNamePrefix(int id) { EnsureValid(id); return id.ToString($"D{FileNameWidth}"); }
        public static bool TryParseFileNamePrefix(string value, out int id)
        {
            id = default;
            return value != null && value.Length == FileNameWidth && int.TryParse(value, out id) && IsValid(id);
        }
    }
}
