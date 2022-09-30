using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MintyRPC;

namespace MintyWPF.Functions {
    public static class AppVars {
        public static bool IsWindows => Environment.OSVersion.ToString().ToLower().Contains("windows");
    }

    public static class StartTasks {
        private static readonly string SdkFilePath = $"{Environment.CurrentDirectory}/discord_game_sdk.{(AppVars.IsWindows ? "dll" : "so")}";

        public static void OnApplicationStart() {
            ConfigSetup.Setup();

            if (File.Exists(SdkFilePath)) return;
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetCallingAssembly()
                .GetManifestResourceNames().First(x => x.EndsWith($"discord_game_sdk.{(AppVars.IsWindows ? "dll" : "so")}")));
            var file = new byte[stream!.Length];
            stream.Read(file, 0, (int)stream.Length);
            stream.Close();

            File.WriteAllBytes(SdkFilePath, file);
        }

        public static bool IsUpdateAvailable;
        private static readonly string LazyTag = "\"tag_name\": \"v2.0.0\"";
        private static readonly string GitHub = "https://api.github.com/repos/Minty-Labs/MintyRPC/releases";

        public static void CheckForAppUpdate() {
            var s = GetString().GetAwaiter().GetResult();
            IsUpdateAvailable = !s.Contains(LazyTag);
        }

        private static async Task<string> GetString() {
            var web = new HttpClient();
            web.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36 Edg/105.0.1343.53");
            // web.DefaultRequestHeaders.Add("Content-Type", "application/json");
            return await web.GetStringAsync(GitHub);
        }
    }
}
