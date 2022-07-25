using System.Diagnostics;
using System.Runtime.InteropServices;
using Discord;
using System.Text;
using Activity = Discord.Activity;
using Yggdrasil.Logging;
using Yggdrasil.Util.Commands;

namespace MintyRPC;

public static class BuildInfo {
    public const string Name = "MintyRPC";
    public const string Version = "0.0.14";
    public const string Author = "Lily";
    public const string Company = "Minty Labs";
    public static bool IsWindows => Environment.OSVersion.ToString().ToLower().Contains("windows");
}

public class Program {
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int AppWindowHide = 0;
    private const int AppWindowShow = 5;
    private static IntPtr _handle;
    
    private static bool _isRunning, _firstStart, _initStart;
    private static Discord.Discord? _discord;
    private static Thread _loopThread = new (CallBack);
    private static long _time;
    private static DateTime _startTime;
    private static Activity _activity;
    private static ActivityManager? _activityManager;
    private static ApplicationManager? _applicationManager;
    private static string? _randomLobbyId;
    
    public static void Main(string[] args) {
        if (!BuildInfo.IsWindows) {
            Console.Write("This application is not meant to run on Linux, press any key to exit...");
            Console.ReadLine();
            Process.GetCurrentProcess().Kill();
            return;
        }
        _handle = GetConsoleWindow();
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = $"{BuildInfo.Name} v{BuildInfo.Version} - Not Running";
        Utils.WriteDiscordGameSdkDll();
        ConfigSetup.Setup();

        ConfigSetup.staticDetails = ConfigSetup.GetPresenceInfo().Details;
        ConfigSetup.staticState = ConfigSetup.GetPresenceInfo().State;

        var cmds = new ConsoleCommands();
        
        cmds.Add("start", "Starts the Discord Rich Presence", StartDiscord);
        cmds.Add("setautostart", "Allow the Discord Rich Presence to automatically start when you open this application [T/F]", SetAutoStart);
        cmds.Add("setautorestart", "Automatically restart the Discord Rich Presence if it crashes or closes randomly [T/F]", SetAutoRestart);
        cmds.Add("settoruninbackground", "Set the application to automatically run in the background when the Discord Rich Presence is running [T/F]", SetToRunInBackground);
        cmds.Add("setstate", "Sets the state of the Discord Rich Presence", SetState);
        cmds.Add("setdetails", "Sets the details of the Discord Rich Presence", SetDetails);
        cmds.Add("setlargeimage", "Sets the large image of the Discord Rich Presence", SetLargeImage);
        cmds.Add("setlargetooltip", "Sets the large image text of the Discord Rich Presence", SetLargeImageText);
        cmds.Add("setsmallimage", "Sets the small image of the Discord Rich Presence", SetSmallImage);
        cmds.Add("setsmalltooltip", "Sets the small image text of the Discord Rich Presence", SetSmallImageText);
        cmds.Add("setpartysizecurrent", "Sets the party size current of the Discord Rich Presence", SetPartySizeCurrent);
        cmds.Add("setpartysizemax", "Sets the party size max of the Discord Rich Presence", SetPartySizeMax);
        cmds.Add("sendtobackground", "Sends the application to run in the background, hiding the window - use Task Manager to kill the app", SendToBackground);
        cmds.Add("kill", "Kills the Discord Rich Presence", KillDiscord);
        
        _initStart = true;
        if (!_isRunning && _initStart)
            if (ConfigSetup.GetGeneralInfo().AutoStart)
                BasicStartDiscord();
        
        cmds.Wait(); // Keep at the bottom of the method, anything after this will not be executed
    }

    #region Commands
    
    private static CommandResult SetToRunInBackground(string command, Arguments args) {
        var argsString = args.ToString();
        var @true = argsString!.ToLower().Contains("true") || argsString.ToLower().Contains('t');
        if (!@true) return CommandResult.Okay;
        
        Log.Info("This setting will require AutoStart to be set to true.");
        Console.Write("Are you sure you want to run the application in the background on startup? [Y/N]");
        var input = Console.ReadLine();
        if (!input!.ToLower().Contains("yes") && !input.ToLower().Contains('y')) return CommandResult.Break;
        
        ConfigSetup.GetGeneralInfo().RunInBackground = true;
        ConfigSetup.Save();

        return CommandResult.Break;
    }

    private static CommandResult SendToBackground(string command, Arguments args) {
        ShowWindow(_handle, AppWindowHide);
        return CommandResult.Okay;
    }

    private static CommandResult SetAutoStart(string command, Arguments args) {
        var argsString = args.ToString();
        var @true = argsString!.ToLower().Contains("true") || argsString.ToLower().Contains('t');
        if (!@true) {
            ConfigSetup.GetGeneralInfo().AutoStart = false;
            ConfigSetup.Save();
            return CommandResult.Okay;
        }
        ConfigSetup.GetGeneralInfo().AutoStart = true;
        ConfigSetup.Save();
        Log.Info($"Set AutoStart to {ConfigSetup.GetGeneralInfo().AutoStart}");
        return CommandResult.Okay;
    }

    private static CommandResult SetAutoRestart(string command, Arguments args) {
        var argsString = args.ToString();
        var @true = argsString!.ToLower().Contains("true") || argsString.ToLower().Contains('t');
        if (!@true) {
            ConfigSetup.GetGeneralInfo().AutoRestart = false;
            ConfigSetup.Save();
            return CommandResult.Okay;
        }
        ConfigSetup.GetGeneralInfo().AutoRestart = true;
        ConfigSetup.Save();
        Log.Info($"Set AutoRestart to {ConfigSetup.GetGeneralInfo().AutoRestart}");
        return CommandResult.Okay;
    }

    private static CommandResult StartDiscord(string command, Arguments args) {
        if (_isRunning) return CommandResult.Break;
        Log.Info("Starting Discord Rich Presence, please wait...");
        
        var processes = Process.GetProcesses();
        var isDiscordAppRunning = processes.Any(p => p.ProcessName is "Discord" or "DiscordPTB" or "DiscordCanary");
        
        if (!isDiscordAppRunning) {
            Log.Warning("Discord is not running.");
            // Process.GetCurrentProcess().Kill();
            return CommandResult.Okay;
        }
        Console.Title = $"{BuildInfo.Name} v{BuildInfo.Version} - Starting, please wait...";
        _startTime = DateTime.Now;
        _time = GetTimeAsLong(ConfigSetup.GetPresenceInfo().StartTimestamp);

        var tempLobbyId = "";
        if (string.IsNullOrWhiteSpace(ConfigSetup.GetPresenceInfo().LobbyId)) {
            tempLobbyId = GenerateRandomString(69);
            ConfigSetup.GetPresenceInfo().LobbyId = tempLobbyId;
            ConfigSetup.Save();
        }
        _randomLobbyId = tempLobbyId;
        
        var clientId = Environment.GetEnvironmentVariable("702767245385924659") ?? "702767245385924659";
        _discord = new Discord.Discord(Int64.Parse(clientId), (UInt64)Discord.CreateFlags.Default);
        
        _discord.SetLogHook(Discord.LogLevel.Debug, (level, message) => {
            Log.Status($"Rich Presence has started with code: {message}");
        });
        
        _applicationManager = _discord.GetApplicationManager();
        
        UpdateActivity();
        
        _loopThread.Start();
        _isRunning = true;
        if (ConfigSetup.GetGeneralInfo().AutoRestart && ConfigSetup.GetGeneralInfo().RunInBackground)
            ShowWindow(_handle, AppWindowHide);
        return CommandResult.Okay;
    }

    private static void BasicStartDiscord(bool callFromRestart = false) {
        if (_isRunning) return;
        Log.Info($"{(callFromRestart ? "Res" : "S")}tarting Discord Rich Presence, please wait...");

        if (!callFromRestart) {
            var processes = Process.GetProcesses();
            var isDiscordAppRunning = processes.Any(p => p.ProcessName is "Discord" or "DiscordPTB" or "DiscordCanary");
        
            if (!isDiscordAppRunning) {
                Log.Warning("Discord is not running.");
                // Process.GetCurrentProcess().Kill();
                return;
            }
        }
        
        Console.Title = $"{BuildInfo.Name} v{BuildInfo.Version} - Starting, please wait...";
        _startTime = DateTime.Now;
        _time = GetTimeAsLong(ConfigSetup.GetPresenceInfo().StartTimestamp);

        var tempLobbyId = "";
        if (string.IsNullOrWhiteSpace(ConfigSetup.GetPresenceInfo().LobbyId)) {
            tempLobbyId = GenerateRandomString(69);
            ConfigSetup.GetPresenceInfo().LobbyId = tempLobbyId;
            ConfigSetup.Save();
        }
        _randomLobbyId = tempLobbyId;
        
        var clientId = Environment.GetEnvironmentVariable("702767245385924659") ?? "702767245385924659";
        _discord = new Discord.Discord(Int64.Parse(clientId), (UInt64)Discord.CreateFlags.Default);
        
        _discord.SetLogHook(Discord.LogLevel.Debug, (level, message) => {
            Log.Status($"Rich Presence has {(callFromRestart ? "re" : "")}started with code: {message}");
        });
        
        _applicationManager = _discord.GetApplicationManager();
        
        UpdateActivity();
        
        if (!callFromRestart)
            _loopThread.Start();
        _isRunning = true;
        if (ConfigSetup.GetGeneralInfo().RunInBackground)
            ShowWindow(_handle, AppWindowHide);
    }

    private static long GetTimeAsLong(long time) 
        => time switch {
            123456789 => (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
            100000001 => (long)DateTime.UtcNow.Subtract(new TimeSpan(_startTime.Hour, _startTime.Minute, _startTime.Second))
                .Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
            1 => (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
            _ => ConfigSetup.GetPresenceInfo().StartTimestamp
        };

    private static CommandResult SetDetails(string command, Arguments args) {
        var sb = new StringBuilder();
        var words = args.GetAll();
        foreach (var word in words) {
            sb.Append($"{word} ").Replace("setdetails ", "");
        }
        
        ConfigSetup.GetPresenceInfo().Details = sb.ToString();
        ConfigSetup.Save();
        
        UpdateActivity();
        
        _activityManager?.UpdateActivity(_activity, result => Log.Info("Updated activity"));
        return CommandResult.Okay;
    }

    private static CommandResult SetState(string command, Arguments args) {
        var sb = new StringBuilder();
        var words = args.GetAll();
        foreach (var word in words) {
            sb.Append($"{word} ").Replace("setstate ", "");
        }
        
        ConfigSetup.GetPresenceInfo().State = sb.ToString();
        ConfigSetup.Save();
        
        UpdateActivity();
        
        _activityManager?.UpdateActivity(_activity, result => Log.Info("Updated activity"));
        return CommandResult.Okay;
    }
    
    private static CommandResult SetLargeImage(string command, Arguments args) {
        var sb = new StringBuilder();
        var words = args.GetAll();
        foreach (var word in words) {
            sb.Append($"{word} ").Replace("setlargeimage ", "").Replace(" ", "");
        }
        
        ConfigSetup.GetPresenceInfo().LargeImageKey = sb.ToString().Trim();
        ConfigSetup.Save();
        
        UpdateActivity();
        
        _activityManager?.UpdateActivity(_activity, result => Log.Info("Updated activity"));
        return CommandResult.Okay;
    }
    
    private static CommandResult SetLargeImageText(string command, Arguments args) {
        var sb = new StringBuilder();
        var words = args.GetAll();
        foreach (var word in words) {
            sb.Append($"{word} ").Replace("setlargetooltip ", "");
        }
        
        ConfigSetup.GetPresenceInfo().LargeImageTooltipText = sb.ToString().Trim();
        ConfigSetup.Save();
        
        UpdateActivity();
        
        _activityManager?.UpdateActivity(_activity, result => Log.Info("Updated activity"));
        return CommandResult.Okay;
    }
    
    private static CommandResult SetSmallImage(string command, Arguments args) {
        var sb = new StringBuilder();
        var words = args.GetAll();
        foreach (var word in words) {
            sb.Append($"{word} ").Replace("setsmallimage ", "").Replace(" ", "");
        }
        
        ConfigSetup.GetPresenceInfo().SmallImageKey = sb.ToString().Trim();
        ConfigSetup.Save();
        
        UpdateActivity();
        
        _activityManager?.UpdateActivity(_activity, result => Log.Info("Updated activity"));
        return CommandResult.Okay;
    }
    
    private static CommandResult SetSmallImageText(string command, Arguments args) {
        var sb = new StringBuilder();
        var words = args.GetAll();
        foreach (var word in words) {
            sb.Append($"{word} ").Replace("setsmalltooltip ", "");
        }
        
        ConfigSetup.GetPresenceInfo().SmallImageTooltipText = sb.ToString().Trim();
        ConfigSetup.Save();
        
        UpdateActivity();
        
        _activityManager?.UpdateActivity(_activity, result => Log.Info("Updated activity"));
        return CommandResult.Okay;
    }
    
    private static CommandResult SetPartySizeCurrent(string command, Arguments args) {
        var sb = new StringBuilder();
        var words = args.GetAll();
        foreach (var word in words) {
            sb.Append($"{word} ").Replace("setpartysizecurrent ", "").Replace(" ", "");
        }
        
        ConfigSetup.GetPresenceInfo().CurrentSize = int.Parse(sb.ToString());
        ConfigSetup.Save();
        
        UpdateActivity();
        
        _activityManager?.UpdateActivity(_activity, result => Log.Info("Updated activity"));
        return CommandResult.Okay;
    }
    
    private static CommandResult SetPartySizeMax(string command, Arguments args) {
        var sb = new StringBuilder();
        var words = args.GetAll();
        foreach (var word in words) {
            sb.Append($"{word} ").Replace("setpartysizemax ", "").Replace(" ", "");
        }
        
        ConfigSetup.GetPresenceInfo().MaxSize = int.Parse(sb.ToString());
        ConfigSetup.Save();
        
        UpdateActivity();
        
        _activityManager?.UpdateActivity(_activity, result => Log.Info("Updated activity"));
        return CommandResult.Okay;
    }

    private static CommandResult KillDiscord(string command, Arguments args) {
        _firstStart = false;
        _isRunning = false;
        Console.Title = $"{BuildInfo.Name} v{BuildInfo.Version} - Not Running";
        _activityManager?.ClearActivity(s => Log.Info("Cleared activity"));
        return CommandResult.Okay;
    }

    private static void BasicKillDiscord() {
        _firstStart = false;
        _isRunning = false;
        Console.Title = $"{BuildInfo.Name} v{BuildInfo.Version} - Not Running";
        _activityManager?.ClearActivity(s => Log.Info("Cleared activity"));
    }
    
    #endregion
    
    private static void CallBack() {
        try {
            while (true) {
                if (!_isRunning) return;
                
                _discord?.RunCallbacks();
                Console.Title = $"{BuildInfo.Name} v{BuildInfo.Version} - Details: {ConfigSetup.staticDetails} - State: {ConfigSetup.staticState} - Memory Usage: " + Math.Round(GC.GetTotalMemory(false) / 1024f) + " KB";
                
                if (DateTime.Now == new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 01)) {
                    _time = GetTimeAsLong(ConfigSetup.GetPresenceInfo().StartTimestamp);
                    UpdateActivity();
                    Log.Info("Auto reset time.");
                }
                
                Thread.Sleep(1000 / 60);
            }
        }
        catch (Exception e) {
            _isRunning = false;
            var ee = e.ToString().Replace("C:\\Users\\dephi\\Documents\\Visual Studio Projects\\MintyRPC\\", "...");
            Log.Error($"Unable to run CallBack(): \n{ee}");
            
            if (ConfigSetup.GetGeneralInfo().AutoRestart && ConfigSetup.GetGeneralInfo().RunInBackground)
                ShowWindow(_handle, AppWindowShow);
            
            if (e.ToString().Contains("NotRunning")) 
                BasicKillDiscord();
        }
        finally {
            _discord?.Dispose();
            if (!_isRunning && _initStart)
                if (ConfigSetup.GetGeneralInfo().AutoRestart)
                    BasicStartDiscord(true);
        }
    }

    // private static async Task RestartPresence() {
    //     await Task.Delay(10 * 1000);
    //     StartDiscord("start", new Arguments());
    // }

    private static void UpdateActivity() {
        
        _activityManager = _discord?.GetActivityManager();
        var lobbyManager = _discord?.GetLobbyManager();

        var noParty = ConfigSetup.GetPresenceInfo().CurrentSize == 0 && ConfigSetup.GetPresenceInfo().MaxSize == 0;

        if (noParty) {
            _activity = new Activity {
                Name = "MintyRPC",
                Timestamps = new ActivityTimestamps {
                    Start = _time,
                    End = 0
                },
                Details = ConfigSetup.GetPresenceInfo().Details ?? "", /* Top Text */
                State = ConfigSetup.GetPresenceInfo().State ?? "",     /* Bottom Text */
                Assets = new ActivityAssets {
                    LargeImage = ConfigSetup.GetPresenceInfo().LargeImageKey ?? "",
                    LargeText = ConfigSetup.GetPresenceInfo().LargeImageTooltipText ?? "",
                    SmallImage = ConfigSetup.GetPresenceInfo().SmallImageKey ?? "",
                    SmallText = ConfigSetup.GetPresenceInfo().SmallImageTooltipText ?? ""
                },
                Instance = true
            };
        }
        else {
            _activity = new Activity {
                Name = "MintyRPC",
                Timestamps = new ActivityTimestamps {
                    Start = _time,
                    End = 0
                },
                Details = ConfigSetup.GetPresenceInfo().Details ?? "", /* Top Text */
                State = ConfigSetup.GetPresenceInfo().State ?? "",     /* Bottom Text */
                Assets = new ActivityAssets {
                    LargeImage = ConfigSetup.GetPresenceInfo().LargeImageKey ?? "",
                    LargeText = ConfigSetup.GetPresenceInfo().LargeImageTooltipText ?? "",
                    SmallImage = ConfigSetup.GetPresenceInfo().SmallImageKey?? "",
                    SmallText = ConfigSetup.GetPresenceInfo().SmallImageTooltipText ?? ""
                },
                Party = {
                    Id = _randomLobbyId!,
                    Size = {
                        CurrentSize = ConfigSetup.GetPresenceInfo().CurrentSize,
                        MaxSize = ConfigSetup.GetPresenceInfo().MaxSize
                    }
                },
                Instance = true
            };
        }
            
        _activityManager?.UpdateActivity(_activity, result => {
            Log.Info("Activity updated: {0}", result);
            
            if (_firstStart) return;
            
            var t = DateTime.Now - _startTime;
            Log.Status($"Discord Rich Presence started in {t.Milliseconds}ms.");
            // Console.Title = $"{BuildInfo.Name} v{BuildInfo.Version} - Details: {ConfigSetup.staticDetails} - State: {ConfigSetup.staticState}"; // Kinda redundant
            _firstStart = true;
        });
    }
    
    private static string GenerateRandomString(int length) {
        const string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var array = new char[length];
        var random = new Random();
        for (var i = 0; i < length; i++)  {
            array[i] = text[random.Next(text.Length)];
        }
        return new string(array);
    }
}