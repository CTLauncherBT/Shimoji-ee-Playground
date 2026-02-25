using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace ShimojiPlaygroundApp
{

    public partial class TutorialWindow : Window
    {
        private EditorSettings settings;

        public bool CheckedTutorial { get; private set; }
        public TutorialWindow(EditorSettings settings)
        {
            InitializeComponent();
            this.settings = settings;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://shimoji.nasumicraft.de/tutorial/how-to-use-playground",
                UseShellExecute = true
            });
            CheckedTutorial = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            CheckedTutorial = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CheckedTutorial = false;
            Close();
        }

        private void DragBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}