using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LMUElectronicBridge
{
    public partial class SettingsControl : UserControl
    {
        public LMUElectronicBridge Plugin { get; }

        public SettingsControl(LMUElectronicBridge plugin)
        {
            InitializeComponent();
            Plugin = plugin;
            DataContext = plugin;
        }

        private void SyncAll_Click(object sender, RoutedEventArgs e) => Plugin.SyncAllFromLMU();
        private void ApplyManual_Click(object sender, RoutedEventArgs e) => Plugin.ApplyManualValues();

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            string path = "";

            switch (btn.Tag.ToString())
            {
                case "TC_Main": path = Plugin.Settings.PropPath_TC_Main; break;
                case "TC_Cut": path = Plugin.Settings.PropPath_TC_Cut; break;
                case "TC_Slip": path = Plugin.Settings.PropPath_TC_Slip; break;
                case "ABS": path = Plugin.Settings.PropPath_ABS; break;
            }

            if (Plugin.TestProperty(path, out string msg))
            {
                btn.Background = Brushes.Green;
                btn.Content = "OK";
                MessageBox.Show(msg, "Property Found Successfully");
            }
            else
            {
                btn.Background = Brushes.Red;
                btn.Content = "Error";
                MessageBox.Show(msg, "Property Name Not Found");
            }
        }

        private void ResetPaths_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Reset all names to LMU REDADeg defaults?", "Confirm Reset", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                Plugin.Settings.PropPath_TC_Main = "lmuDataPlugin.Redadeg.lmu.Extended.VM_TRACTIONCONTROLMAP";
                Plugin.Settings.PropPath_TC_Cut = "lmuDataPlugin.Redadeg.lmu.Extended.VM_TRACTIONCONTROLPOWERCUTMAP";
                Plugin.Settings.PropPath_TC_Slip = "lmuDataPlugin.Redadeg.lmu.Extended.VM_TRACTIONCONTROLSLIPANGLEMAP";
                Plugin.Settings.PropPath_ABS = "lmuDataPlugin.Redadeg.lmu.Extended.VM_ANTILOCKBRAKESYSTEMMAP";
                Plugin.OnPropertyChanged(nameof(Plugin.Settings));
            }
        }
    }
}