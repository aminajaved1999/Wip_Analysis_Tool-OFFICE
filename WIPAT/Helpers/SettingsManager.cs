using System.IO;
using System.Text.Json;

namespace WIPAT.Helpers
{
    public class AppSettings
    {
        public bool RememberMe { get; set; }
        public string SavedUsername { get; set; }
    }

    public static class SettingsManager
    {
        // This saves a tiny file named 'user_settings.json' in the same folder as application's .exe
        private static readonly string filePath = "user_settings.json";

        public static AppSettings Load()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<AppSettings>(json);
            }
            // Return empty settings if the file doesn't exist yet (e.g., first time running the app)
            return new AppSettings { RememberMe = false, SavedUsername = string.Empty };
        }

        public static void Save(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings);
            File.WriteAllText(filePath, json);
        }
    }
}