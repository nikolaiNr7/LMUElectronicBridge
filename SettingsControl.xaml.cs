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

        private async void SyncAll_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            try
            {
                if (btn != null) btn.IsEnabled = false;
                await Plugin.SyncAllFromLMU().ConfigureAwait(true);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
    }
}