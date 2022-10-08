using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.VisualBasic.CompilerServices;
using MintyRPC;
using Activity = Discord.Activity;

namespace MintyWPF;

public class DiscordActivityManager {
    public static bool isRunning, firstStart, initStart;
    private static bool _isDiscordAppRunning(IEnumerable<Process> processes) => processes.Any(p => p.ProcessName is "DiscordManager" or "DiscordPTB" or "DiscordCanary");
    public static Discord.Discord? DiscordManager;
    private static Thread _loopThread = new(CallBack);
    private static long _time;
    public static DateTime startTime;
    private static Activity _activity;
    public static ActivityManager? _activityManager;
    private static ApplicationManager? _applicationManager;
    private static string? _randomLobbyId;
    public static string clientId;

    /*public static void StartPresence() {
        if (isRunning) return;
        var pId = ConfigSetup.GetPresenceInfo().PresenceId;
        if (pId == "0" || !string.IsNullOrWhiteSpace(pId)) return;

        var processes = Process.GetProcesses();
        var isDiscordAppRunning = _isDiscordAppRunning(processes);

        if (!isDiscordAppRunning) throw new Exception();
        startTime = DateTime.Now;
        _time = Functions.Utils.UpdateTimeSelection();

        var tempLobbyId = "";
        if (string.IsNullOrWhiteSpace(ConfigSetup.GetPresenceInfo().LobbyId)) {
            tempLobbyId = Functions.Utils.GenerateRandomString(69);
            ConfigSetup.GetPresenceInfo().LobbyId = tempLobbyId;
            ConfigSetup.Save();
        }
        _randomLobbyId = tempLobbyId;

        //var clientId = Environment.GetEnvironmentVariable() ?? "702767245385924659";
        var _clientId = ConfigSetup.GetPresenceInfo().PresenceId ?? "702767245385924659";
        DiscordManager = new Discord.Discord(Int64.Parse(clientId??_clientId), (UInt64)Discord.CreateFlags.Default);

        DiscordManager.SetLogHook(Discord.LogLevel.Debug, (level, message) => {
            // Log.Status($"Rich Presence has started with code: {message}");
        });

        _applicationManager = DiscordManager.GetApplicationManager();

        UpdateActivity();

        //if (!firstStart)
            _loopThread.Start();
        isRunning = true;
    }*/

    public static void BasicStartDiscord(bool callFromRestart = false) {
        if (isRunning)
            return;

        var pId = ConfigSetup.GetPresenceInfo().PresenceId;
        if (pId == 0)
            return;

        if (!callFromRestart) {
            Task.Run(() => {
                var processes = Process.GetProcesses();
                var isDiscordAppRunning = _isDiscordAppRunning(processes);

                if (!isDiscordAppRunning) {
                    Task.Delay(2000);
                    if (!isDiscordAppRunning) {
                        Task.Delay(2000);
                        if (!isDiscordAppRunning) {
                            Task.Delay(2000);
                            if (!isDiscordAppRunning) {
                                Task.Delay(2000);
                                if (!isDiscordAppRunning) {
                                    Task.Delay(2000);
                                } else {
                                    //Log.Info("Waited 10 seconds. Failed to start Discord Rich Presence, Discord is not running.");
                                    //return;
                                    throw new Exception();
                                }
                            }
                        }
                    }
                }
                
                startTime = DateTime.Now;
                _time = Functions.Utils.UpdateTimeSelection();

                var tempLobbyId = "";
                if (string.IsNullOrWhiteSpace(ConfigSetup.GetPresenceInfo().LobbyId)) {
                    tempLobbyId = Functions.Utils.GenerateRandomString(69);
                    ConfigSetup.GetPresenceInfo().LobbyId = tempLobbyId;
                    ConfigSetup.Save();
                }
                _randomLobbyId = tempLobbyId;

                DiscordManager = new Discord.Discord(Int64.Parse(clientId ?? "702767245385924659"), (UInt64)Discord.CreateFlags.Default);

                DiscordManager.SetLogHook(Discord.LogLevel.Debug, (level, message) => {
                    // Elly is cute
                });

                _applicationManager = DiscordManager.GetApplicationManager();

                UpdateActivity(callFromRestart);

                if (!callFromRestart && !firstStart)
                    _loopThread.Start();
                isRunning = true;
            });
        }
    }

    private static void CallBack() {
        try {
            while (true) {
                if (!isRunning)
                    return;

                DiscordManager?.RunCallbacks();

                if (DateTime.Now == new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 01)) {
                    _time = Functions.Utils.UpdateTimeSelection();
                    UpdateActivity();
                    //Log.Info("Auto reset time.");
                }

                Thread.Sleep(1000 / 60);
            }
        } catch (Exception e) {
            isRunning = false;
            /*var ee = e.ToString().Replace("C:\\Users\\dephi\\Documents\\Visual Studio Projects\\MintyRPC\\", "...");
            Log.Error($"Unable to run CallBack(): \n{ee}");*/

            /*if (ConfigSetup.GetGeneralInfo().AutoRestart && ConfigSetup.GetGeneralInfo().RunInBackground)
                ShowWindow(_handle, AppWindowShow);*/

            if (e.ToString().Contains("NotRunning"))
                BasicKillDiscord();
        } finally {
            DiscordManager?.Dispose();
            if (!isRunning && initStart)
                if (ConfigSetup.GetGeneralInfo().AutoRestart)
                    BasicStartDiscord(true);
        }
    }

    public static void BasicKillDiscord() {
        //firstStart = false;
        isRunning = false;
        _activityManager?.ClearActivity(s => {});
    }

    public static void UpdateActivity(bool callFromRestart = false) {
        _activityManager = DiscordManager?.GetActivityManager();
        var lobbyManager = DiscordManager?.GetLobbyManager();

        _time = Functions.Utils.UpdateTimeSelection();

        if (!ConfigSetup.GetPresenceInfo().EnableParty) {
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
        } else {
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
            /*if (callFromRestart)
                Log.Info("Restarted activity and DiscordManager Rich Presence.");

            Log.Info("Activity updated: {0}", result);*/

            if (firstStart)
                return;

            // var t = DateTime.Now - startTime;
            // Log.Status($"DiscordManager Rich Presence started in {t.Milliseconds}ms.");
            // Console.Title = $"{BuildInfo.Name} v{BuildInfo.Version} - Details: {ConfigSetup.staticDetails} - State: {ConfigSetup.staticState}"; // Kinda redundant
            firstStart = true;
        });
    }
}
