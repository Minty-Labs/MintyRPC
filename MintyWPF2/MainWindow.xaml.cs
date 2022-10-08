using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using MintyRPC;
using MintyWPF.Functions;
using ModernWpf.Controls;
using ComboBox = System.Windows.Controls.ComboBox;
using Path = System.IO.Path;
using TextBox = System.Windows.Controls.TextBox;

namespace MintyWPF {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();

        public int GetTimeStampComboBoxNum() {
            if (NoTimestampItem.IsSelected)
                return 0;
            if (CustomTimstampItem.IsSelected)
                return 1;
            if (ElapsedTimestampItem.IsSelected)
                return 2;
            if (LocalTimestampItem.IsSelected)
                return 3;
            return 99;
        }

        public ComboBoxItem GetBoxItem(int num) {
            return num switch {
                0 => NoTimestampItem,
                1 => CustomTimstampItem,
                2 => ElapsedTimestampItem,
                3 => LocalTimestampItem,
                _ => throw new Exception()
            };
        }

        public MainWindow() {
            StartTasks.OnApplicationStart();
            InitializeComponent();

            LobbyIDBox.ToolTip = ConfigSetup.GetPresenceInfo().LobbyId;
            
            DiscordActivityManager.clientId = ConfigSetup.GetPresenceInfo().PresenceId.ToString();

            if (ConfigSetup.GetGeneralInfo().AutoStart)
                DiscordActivityManager.BasicStartDiscord();
        }

        private void UpdateLabel_OnInitialized(object? sender, EventArgs e) {
            new Thread(StartTasks.CheckForAppUpdate).Start();

            Task.Run(async () => {
                await Task.Delay(1200);
                if (StartTasks.IsUpdateAvailable)
                    UpdateLabel.Visibility = Visibility.Visible;
            });
        }

        private void ClientID_Init(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().PresenceId.ToString();
        }

        private void Details_Init(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().Details;
        }

        private void State_Init(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().State;
        }

        private void LobbyID_Init(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().LobbyId;
        }
        private void LargeImageTooltip_Init(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().LargeImageTooltipText;
        }

        private void LargeImageKey_Init(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().LargeImageKey;
            /*var cb = (ComboBox)sender!;
            cb.Items.Add("Test 1");

            var d = DiscordActivityManager.DiscordManager!.ImageManagerInstance;
            var handle = new Discord.ImageHandle() {
                Id = (Int64)ConfigSetup.GetPresenceInfo().PresenceId,
                Size = 1024
            };
            d.Fetch(handle, false, (result, handleResult) => {
                if (result == Discord.Result.Ok) {
                    var data = d.GetData(handle);

                    for (int i = 0; i < data.Length; i++) {
                        cb.Items.Add(data[i].ToString());
                    }
                }
            });*/
        }

        private void SmallImageTooltip_Init(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().SmallImageTooltipText;
        }

        private void SmallImageKey_Init(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().SmallImageKey;
            /*var cb = (ComboBox)sender!;
            cb.Items.Add("Test 1");*/
        }

        private void CurrentPartyLabel(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().CurrentSize.ToString();
        }

        private void MaxPartyLabel(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().MaxSize.ToString();
        }

        private void DoNotAcceptStrings(object sender, DependencyPropertyChangedEventArgs e) {
            var tb = (TextBox)sender;
            var _string = tb.Text;
            try { var a = int.Parse(_string); } catch { tb.Text = ""; }
        }

        private void OnPartyToggle(object sender, DependencyPropertyChangedEventArgs e) {
            PartyNumbers.IsEnabled = (bool)e.NewValue;
        }

        private void CreatorLink(object sender, MouseButtonEventArgs e) => Process.Start("cmd", "/C start https://github.com/MintLily");
        private void GitHubLink(object sender, MouseButtonEventArgs e) => Process.Start("cmd", "/C start https://github.com/Minty-Labs/MintyRPC");
        private void UpdateLink(object sender, MouseButtonEventArgs e) => Process.Start("cmd",
            "/C start https://github.com/Minty-Labs/MintyRPC/releases");

        private void PartyToggle_Init(object? sender, EventArgs e) {
            var ts = (ToggleSwitch)sender!;
            ts.IsOn = ConfigSetup.GetPresenceInfo().EnableParty;
        }

        private void TimpstampCustomBox_OnInitialized(object? sender, EventArgs e) {
            var tb = (TextBox)sender!;
            tb.Text = ConfigSetup.GetPresenceInfo().StartTimestamp.ToString();
            TimpstampCustomBox.IsEnabled = ConfigSetup.GetPresenceInfo().TimestampPresetSelection == 1;
        }

        private void TimeStampComboBox_OnInitialized(object? sender, EventArgs e) {
            var cb = (ComboBox)sender!;
            cb.SelectedItem = GetBoxItem(ConfigSetup.GetPresenceInfo().TimestampPresetSelection);
        }

        private void TimeStampComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {

            try {
                TimpstampCustomBox.IsEnabled = CustomTimstampItem.IsSelected;
            } catch {
                /* For some reason, it errors on start, but is fine after, so empty trycatch it is */
            }
        }


















        private void SaveAndApply_OnClick(object sender, RoutedEventArgs e) {
            ConfigSetup.GetPresenceInfo().PresenceId = long.Parse(ClientIDBox.Text);
            ConfigSetup.GetPresenceInfo().Details = DetailsBox.Text;
            ConfigSetup.GetPresenceInfo().State = StateBox.Text;
            ConfigSetup.GetPresenceInfo().LargeImageKey = LargeImgBox.Text;
            ConfigSetup.GetPresenceInfo().LargeImageTooltipText = LargeImageToolipBox.Text;
            ConfigSetup.GetPresenceInfo().SmallImageKey = SmallImgBox.Text;
            ConfigSetup.GetPresenceInfo().SmallImageTooltipText = SmallImageToolipBox.Text;
            ConfigSetup.GetPresenceInfo().CurrentSize = int.Parse(CurrentPartyCount.Text);
            ConfigSetup.GetPresenceInfo().MaxSize = int.Parse(MaxPartyCount.Text);
            ConfigSetup.GetPresenceInfo().EnableParty = PartySwitch.IsOn;
            ConfigSetup.GetPresenceInfo().LobbyId = LobbyIDBox.Text;
            ConfigSetup.GetPresenceInfo().TimestampPresetSelection = GetTimeStampComboBoxNum();
            ConfigSetup.GetPresenceInfo().StartTimestamp = long.Parse(TimpstampCustomBox.Text);
            ConfigSetup.Save();
            if (DiscordActivityManager.isRunning)
                DiscordActivityManager.UpdateActivity(true);
        }

        private void StartPresenceButton_OnClick(object sender, RoutedEventArgs e) {
            ClientIDExtraText.Visibility = Visibility.Visible;
            try { DiscordActivityManager.BasicStartDiscord(); } catch{ throw new Exception(); }
        }

        private void StopPresenceButton_OnClick(object sender, RoutedEventArgs e) {
            try { DiscordActivityManager.BasicKillDiscord(); } catch { }
        }

        private void SendToBackground_OnClick(object sender, RoutedEventArgs e) {
            var i = System.Drawing.Icon.ExtractAssociatedIcon(
                $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}MintyRPC.exe");
            
            nIcon.Icon = i;
            nIcon.Visible = true;
            nIcon.Text = "Click to open window.";
            Visibility = Visibility.Hidden;
            nIcon.Click += NIconOnClick;
        }

        private void NIconOnClick(object? sender, EventArgs e) {
            Visibility = Visibility.Visible;
            nIcon.Visible = false;
        }
    }
}
