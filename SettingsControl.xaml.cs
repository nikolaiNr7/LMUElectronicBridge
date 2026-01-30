using System.Windows;
using System.Windows.Controls;

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
            _ = Plugin.SyncAllFromLMU();
        }
    }
}