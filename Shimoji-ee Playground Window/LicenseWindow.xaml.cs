using Microsoft.Win32;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

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
            Logger.Info("Shutting down application...");
            Accepted = false;
            Close();
        }

        private void DragBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}