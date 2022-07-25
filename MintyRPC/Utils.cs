using System.Reflection;

namespace MintyRPC; 

public class Utils {
    private static string _sdkFilePath = $"{Environment.CurrentDirectory}/discord_game_sdk.{(BuildInfo.IsWindows ? "dll" : "so")}";
    
    public static void WriteDiscordGameSdkDll() {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetCallingAssembly()
            .GetManifestResourceNames().First(x => x.EndsWith($"discord_game_sdk.{(BuildInfo.IsWindows ? "dll" : "so")}")));
        var file = new byte[stream!.Length];
        stream.Read(file, 0, (int)stream.Length);
        stream.Close();

        if (!File.Exists(_sdkFilePath)) {
            File.WriteAllBytes(_sdkFilePath, file);
        }
    }
}