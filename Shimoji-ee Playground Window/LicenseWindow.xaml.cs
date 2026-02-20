using Shimoji_ee_Playground_Window.Properties;
using System.Windows;

namespace ShimojiPlaygroundApp
{
    public partial class LicenseWindow : Window
    {
        private EditorSettings settings;
        public bool Accepted { get; private set; }

        public LicenseWindow(EditorSettings settings)
        {
            InitializeComponent();
            this.settings = settings;
            AgreeCheckBox.Checked += (s, e) => AgreeButton.IsEnabled = true;
            AgreeCheckBox.Unchecked += (s, e) => AgreeButton.IsEnabled = false;
        }

        private void Agree_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info($"License accepted: {settings.AcceptedPlaygroundLicense}");
            Logger.Info("Loading EditorWindow...");
            Accepted = true;
            Close();
        }

        private void Disagree_Click(object sender, RoutedEventArgs e)
        {
            Logger.Warn($"License not accepted: {settings.AcceptedPlaygroundLicense}");
            Logger.Info("Shutting down application");
            Accepted = false;
            Close();
        }
    }
}