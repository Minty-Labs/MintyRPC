using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyRPC;

namespace MintyWPF.Functions;

public static class Utils {

    public static long UpdateTimeSelection() {
        if (ConfigSetup.GetPresenceInfo().TimestampPresetSelection == 1)
            return ConfigSetup.GetPresenceInfo().StartTimestamp;

        if (ConfigSetup.GetPresenceInfo().TimestampPresetSelection == 2)
            return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        if (ConfigSetup.GetPresenceInfo().TimestampPresetSelection == 3)
            return (long)DateTime.UtcNow
                .Subtract(new TimeSpan(DiscordActivityManager.startTime.Hour, DiscordActivityManager.startTime.Minute,
                    DiscordActivityManager.startTime.Second)).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        return 0;
    }

    public static string GenerateRandomString(int length) {
        const string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var array = new char[length];
        var random = new Random();
        for (var i = 0; i < length; i++) {
            array[i] = text[random.Next(text.Length)];
        }
        return new string(array);
    }
}
