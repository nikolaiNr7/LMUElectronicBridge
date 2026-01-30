using System.Windows;
using System.Windows.Controls;

namespace LMUElectronicBridge
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        //----- Properties & Members -----------------------------------------
        public LMUElectronicBridge Plugin { get; }

        //----- Constructor --------------------------------------------------
        public SettingsControl(LMUElectronicBridge plugin)
        {
            InitializeComponent();
            Plugin = plugin;

            // Setting the DataContext to the plugin instance allows for 
            // WPF Bindings to work (e.g., {Binding Settings.TC_Main})
            DataContext = plugin;
        }

        //----- UI Event Handlers --------------------------------------------
        /// <summary>
        /// Triggered by the "FORCE SYNC FROM GARAGE" button in the UI.
        /// </summary>
        private void SyncAll_Click(object sender, RoutedEventArgs e)
        {
            // Firing the async sync method without awaiting to keep the UI responsive
            _ = Plugin.SyncAllFromLMU();
        }
    }
}