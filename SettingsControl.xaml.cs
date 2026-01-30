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

        private void SyncAll_Click(object sender, RoutedEventArgs e)
        {
            // Assigning to "_" suppresses warning CS4014 (Fire-and-forget async call)
            _ = Plugin.SyncAllFromLMU();
        }

        private void ApplyManual_Click(object sender, RoutedEventArgs e)
        {
            Plugin.ApplyManualValues();
        }
    }
}