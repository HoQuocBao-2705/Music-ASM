using System.Linq;

namespace Music_ASM.Helpers
{
    public static class UserHelper
    {
        public static string GetInitials(string fullName)
        {
            // Xử lý null hoặc rỗng
            if (string.IsNullOrWhiteSpace(fullName))
                return "?";

            fullName = fullName.Trim();

            // Nếu fullName chỉ có 1 ký tự
            if (fullName.Length == 1)
                return fullName.ToUpper();

            // Lấy chữ cái đầu tiên của chuỗi (không quan tâm tiếng Việt hay tiếng Anh)
            string firstChar = fullName.Substring(0, 1).ToUpper();

            // Xử lý trường hợp đặc biệt: nếu tên có dấu cách, vẫn lấy chữ cái đầu của từ đầu tiên
            var words = fullName.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0 && words[0].Length > 0)
            {
                firstChar = words[0].Substring(0, 1).ToUpper();
            }

            return firstChar;
        }

        public static string GetAvatarColor(string name)
        {
            var colors = new[]
            {
                "#1DB954", // Spotify Green
                "#E91E63", // Pink
                "#9C27B0", // Purple
                "#FF9800", // Orange
                "#00BCD4", // Cyan
                "#F44336", // Red
                "#3F51B5", // Indigo
                "#009688", // Teal
                "#FFC107", // Amber
                "#8BC34A", // Light Green
                "#673AB7", // Deep Purple
                "#FF5722"  // Deep Orange
            };

            int index = System.Math.Abs(name.GetHashCode()) % colors.Length;
            return colors[index];
        }
    }
}