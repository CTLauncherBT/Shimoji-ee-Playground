using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace ShimojiPlaygroundApp
{
    [Serializable]
    public class EditorSettings
    {
        public string WindowTitle = "Shimoji-ee Playground";
        public double WindowWidth = 960;
        public double WindowHeight = 540;
        public string TopOverlayPath = "playgrounds/Scrubland Playground/top.png";
        public string BottomOverlayPath = "";
        public string LeftOverlayPath = "";
        public string RightOverlayPath = "";
        public double TopHeight = 156;
        public double BottomHeight = 0;
        public double LeftWidth = 0;
        public double RightWidth = 0;
        public bool StartDirectPlayground = false;
        public string SelectedPlayground = "Scrubland Playground";
        public bool MainWindowTopMost = true;
        public string BackgroundPath = "playgrounds/Scrubland Playground/assets/main/playground.png";
        public bool AcceptedPlaygroundLicense = false;
    }

    public class PlaygroundItem
    {
        public string Name { get; set; }
        public BitmapImage Icon { get; set; }
        public string Path { get; set; }
    }

    public partial class EditorWindow : Window
    {
        private EditorSettings settings;
        private string settingsFile = "Shimoji-ee_Settings.xml";
        private ObservableCollection<PlaygroundItem> playgrounds = new ObservableCollection<PlaygroundItem>();
        private DispatcherTimer updateTimer;
        private DispatcherTimer animTimer;
        private List<OverlayFrame> animationFrames;
        private int frameIndex;
        private TabItem appTab;
        private StackPanel appsStack;

        public class OverlayFrame
        {
            public TimeSpan Duration;
            public string BackgroundAnim;
            public string TopAnim;
            public string LeftAnim;
            public string RightAnim;
            public string BottomAnim;
        }

        public EditorWindow()
        {

            InitializeComponent();
            Logger.Info($"Started {Title}");
            PlaygroundComboBox.ItemsSource = playgrounds;

            LoadSettings();
            LoadPlaygrounds();
            checkSkipEditor();
            checkLicenseAccepted();
            ApplySettingsToUI();
            LoadPluginsTab();

            updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            updateTimer.Tick += (s, e) => LoadPlaygrounds();
            updateTimer.Start();
        }

        private void LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(EditorSettings));
                    using var stream = File.OpenRead(settingsFile);
                    settings = (EditorSettings)serializer.Deserialize(stream);
                }
                catch { settings = new EditorSettings(); }
            }
            else settings = new EditorSettings();
        }

        private void SaveSettings()
        {
            if (MessageBox.Show("Save settings? (can't be undo)", "Save Settings", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                settings.WindowTitle = WindowTitleTextBox.Text;
                settings.WindowWidth = double.TryParse(WidthTextBox.Text, out double ww) ? ww : 960;
                settings.WindowHeight = double.TryParse(HeightTextBox.Text, out double wh) ? wh : 540;
                settings.TopOverlayPath = TopOverlayText.Text;
                settings.BottomOverlayPath = BottomOverlayText.Text;
                settings.LeftOverlayPath = LeftOverlayText.Text;
                settings.RightOverlayPath = RightOverlayText.Text;
                settings.TopHeight = double.TryParse(TopHeightText.Text, out double th) ? th : 100;
                settings.BottomHeight = double.TryParse(BottomHeightText.Text, out double bh) ? bh : 100;
                settings.LeftWidth = double.TryParse(LeftWidthText.Text, out double lw) ? lw : 50;
                settings.RightWidth = double.TryParse(RightWidthText.Text, out double rw) ? rw : 50;
                settings.StartDirectPlayground = StartDirectPlaygroundCheckBox.IsChecked ?? false;
                settings.SelectedPlayground = PlaygroundComboBox.SelectedItem is PlaygroundItem pi ? pi.Name : "Basic Playground";
                settings.BackgroundPath = BackgroundText.Text;
                settings.MainWindowTopMost = TopMostMainWindowCheckbox.IsChecked ?? false;

                Logger.Info("Saved settings:");
                Logger.Info(settingsFile);

                XmlSerializer serializer = new XmlSerializer(typeof(EditorSettings));
                        using var stream = File.Create(settingsFile);
                        serializer.Serialize(stream, settings);
            }
        }

        private void checkLicenseAccepted()
        {
            if (!settings.AcceptedPlaygroundLicense)
            {
                LicenseWindow license = new LicenseWindow(settings);
                license.ShowDialog();

                if (!license.Accepted)
                {
                    Application.Current.Shutdown();
                    return;
                }

                settings.AcceptedPlaygroundLicense = true;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(EditorSettings));
            using var stream = File.Create(settingsFile);
            serializer.Serialize(stream, settings);
        }

        private void ApplySettingsToUI()
        {
            Logger.Info("Applied Settings to UI");

            WindowTitleTextBox.Text = settings.WindowTitle;
            WidthTextBox.Text = settings.WindowWidth.ToString();
            HeightTextBox.Text = settings.WindowHeight.ToString();
            TopHeightText.Text = settings.TopHeight.ToString();
            BottomHeightText.Text = settings.BottomHeight.ToString();
            LeftWidthText.Text = settings.LeftWidth.ToString();
            RightWidthText.Text = settings.RightWidth.ToString();
            BackgroundText.Text = settings.BackgroundPath;

            var playground = playgrounds
                .FirstOrDefault(p => p.Name == settings.SelectedPlayground);

            if (playground != null)
                PlaygroundComboBox.SelectedItem = playground;

            StartDirectPlaygroundCheckBox.IsChecked = settings.StartDirectPlayground;
            TopMostMainWindowCheckbox.IsChecked = settings.MainWindowTopMost;
        }

        private void LoadPlaygrounds()
        {
            string path = "playgrounds";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var dirs = Directory.GetDirectories(path).Select(d => Path.GetFileName(d)).ToList();

            foreach (var dir in dirs)
            {
                if (!playgrounds.Any(p => p.Name == dir))
                {
                    string folderPath = Path.Combine(path, dir);
                    string iconPath = Path.Combine(folderPath, "icon.png");
                    BitmapImage icon = LoadBitmap(iconPath);

                    playgrounds.Add(new PlaygroundItem
                    {
                        Name = dir,
                        Icon = icon,
                        Path = folderPath
                    });
                }
            }

            for (int i = playgrounds.Count - 1; i >= 0; i--)
            {
                if (!dirs.Contains(playgrounds[i].Name))
                    playgrounds.RemoveAt(i);
            }

            if (PlaygroundComboBox.SelectedItem == null && playgrounds.Any())
            {
                var savedPg = playgrounds.FirstOrDefault(p => p.Name == settings.SelectedPlayground);
                PlaygroundComboBox.SelectedItem = savedPg ?? playgrounds.First();
            }
        }

        private void LoadPluginsTab() // plugin support
        {
            string appsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps");
            if (!Directory.Exists(appsRoot)) Directory.CreateDirectory(appsRoot);

            var appsDirs = Directory.GetDirectories(appsRoot)
                                      .Where(d => File.Exists(Path.Combine(d, "entry.py")))
                                      .ToList();

            if (!appsDirs.Any()) return;

            TabItem appsTab = new TabItem { Header = "Apps" };

            WrapPanel wrap = new WrapPanel
            {
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                ItemWidth = 150,
                ItemHeight = 200,
                Orientation = Orientation.Horizontal
            };

            appsTab.Content = new ScrollViewer
            {
                Content = wrap,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            TabControlMain.Items.Add(appsTab);

            foreach (var dir in appsDirs)
            {
                string appName = Path.GetFileName(dir);
                string entryPath = Path.Combine(dir, "entry.py");
                string iconPath = Path.Combine(dir, "icon.png");

                Border border = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(5),
                    Padding = new Thickness(5),
                    Background = System.Windows.Media.Brushes.Transparent,
                    Width = 150,
                    Height = 200
                };

                StackPanel panel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                TextBlock nameText = new TextBlock
                {
                    Text = appName,
                    Foreground = System.Windows.Media.Brushes.Black,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap
                };
                panel.Children.Add(nameText);

                if (File.Exists(iconPath))
                {
                    Image icon = new Image
                    {
                        Source = LoadBitmap(iconPath),
                        Width = 110,
                        Height = 110,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    panel.Children.Add(icon);
                }

                Button runButton = new Button
                {
                    Content = "Open app",
                    Width = 80,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Tag = entryPath
                };
                runButton.Click += RunApp_Click;
                panel.Children.Add(runButton);

                border.Child = panel;
                wrap.Children.Add(border);
            }
        }

        private BitmapImage LoadBitmap(string path)
        {
            if (!File.Exists(path)) return null;

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private List<OverlayFrame> LoadAnimation(string folderPath)
        {
            var frames = new List<OverlayFrame>();
            string animPath = Path.Combine(folderPath, "animation.txt");
            if (!File.Exists(animPath)) return frames;

            OverlayFrame current = null;
            foreach (var raw in File.ReadAllLines(animPath))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("sec_"))
                {
                    if (current != null) frames.Add(current);
                    double sec = double.Parse(line.Substring(4), System.Globalization.CultureInfo.InvariantCulture);
                    current = new OverlayFrame { Duration = TimeSpan.FromSeconds(sec) };
                }
                else if (line.StartsWith("TopAnim=")) current.TopAnim = Path.Combine(folderPath, line.Substring("TopAnim=".Length));
                else if (line.StartsWith("BottomAnim=")) current.BottomAnim = Path.Combine(folderPath, line.Substring("BottomAnim=".Length));
                else if (line.StartsWith("LeftAnim=")) current.LeftAnim = Path.Combine(folderPath, line.Substring("LeftAnim=".Length));
                else if (line.StartsWith("RightAnim=")) current.RightAnim = Path.Combine(folderPath, line.Substring("RightAnim=".Length));
                else if (line.StartsWith("BackgroundAnim=")) current.BackgroundAnim = Path.Combine(folderPath, line.Substring("BackgroundAnim=".Length));
            }
            if (current != null) frames.Add(current);
            return frames;
        }

        private void OverlayAnimTick(object sender, EventArgs e)
        {
            if (animationFrames == null || animationFrames.Count == 0) return;

            var frame = animationFrames[frameIndex];

            TopOverlayImage.Source = LoadBitmap(frame.TopAnim);
            BottomOverlayImage.Source = LoadBitmap(frame.BottomAnim);
            LeftOverlayImage.Source = LoadBitmap(frame.LeftAnim);
            RightOverlayImage.Source = LoadBitmap(frame.RightAnim);
            BackgroundImage.Source = LoadBitmap(frame.BackgroundAnim);

            frameIndex = (frameIndex + 1) % animationFrames.Count;
            animTimer.Interval = frame.Duration;
        }

        private void PlaygroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaygroundComboBox.SelectedItem is not PlaygroundItem selected) return;

            animationFrames = LoadAnimation(selected.Path);

            string folderPath = selected.Path;

            string playgroundName = selected.Name;

            string mainImg = Path.Combine(folderPath, "playground.png");
            string mainPreview = Path.Combine(folderPath, "preview.png");
            if (File.Exists(mainImg))
            {
                BackgroundText.Text = mainImg;
                PreviewImage.Source = LoadBitmap(mainPreview);
            }
            else
            {
                BackgroundText.Text = "";
                PreviewImage.Source = null;
            }

            TopOverlayText.Text = File.Exists(Path.Combine(folderPath, "top.png")) ? Path.Combine(folderPath, "top.png") : "";
            BottomOverlayText.Text = File.Exists(Path.Combine(folderPath, "bottom.png")) ? Path.Combine(folderPath, "bottom.png") : "";
            LeftOverlayText.Text = File.Exists(Path.Combine(folderPath, "left.png")) ? Path.Combine(folderPath, "left.png") : "";
            RightOverlayText.Text = File.Exists(Path.Combine(folderPath, "right.png")) ? Path.Combine(folderPath, "right.png") : "";

            string settingsTxt = Path.Combine(folderPath, "settings.txt");
            if (File.Exists(settingsTxt))
            {
                Logger.Info($"Loading settings for {playgroundName}");
                foreach (var line in File.ReadAllLines(settingsTxt))
                {

                    // Window settings

                    if (line.StartsWith("WindowHeight=") && double.TryParse(line.Substring(13), out double wh))
                    {
                        HeightTextBox.Text = wh.ToString();
                        Logger.Info($"Loaded setting 'WindowHeight' for Window Height (with the value: {wh})");
                    }
                    else if (line.StartsWith("WindowWidth=") && double.TryParse(line.Substring(12), out double ww))
                    {
                        WidthTextBox.Text = ww.ToString();
                        Logger.Info($"Loaded setting 'WindowWidth' for Window Width (with the value: {ww})");
                    }

                    // Overlay settings

                    else if (line.StartsWith("TopHeight=") && double.TryParse(line.Substring(10), out double th))
                    {
                        TopHeightText.Text = th.ToString();
                        Logger.Info($"Loaded setting 'TopHeight' for Top Widnow Height (with the value: {th})");
                    }
                    else if (line.StartsWith("BottomHeight=") && double.TryParse(line.Substring(13), out double bh))
                    {
                        BottomHeightText.Text = bh.ToString();
                        Logger.Info($"Loaded setting 'BottomHeight' for Bottom Widnow Height (with the value: {bh})");
                    }
                    else if (line.StartsWith("LeftWidth=") && double.TryParse(line.Substring(10), out double lw))
                    {
                        LeftWidthText.Text = lw.ToString();
                        Logger.Info($"Loaded setting 'LeftWidth' for Left Widnow Width (with the value: {lw})");
                    }
                    else if (line.StartsWith("RightWidth=") && double.TryParse(line.Substring(11), out double rw))
                    {
                        RightWidthText.Text = rw.ToString();
                        Logger.Info($"Loaded setting 'RightWidth' for Right Widnow Width (with the value: {rw})");
                    }

                    // Playground settings

                    else if (line.StartsWith("TopMost=") && bool.TryParse(line.Substring(8), out bool tm))
                    {
                        TopMostMainWindowCheckbox.IsChecked = tm;
                        Logger.Info($"Loaded setting 'TopMost' for Main Window (with the value: {tm})");
                    }

                    // Custom Path

                    else if (line.StartsWith("PreviewPath="))
                    {
                        string relativePath = line.Substring("PreviewPath=".Length);
                        mainPreview = Path.Combine(folderPath, relativePath);

                        Logger.Info($"Loaded setting 'PreviewPath' for Main Window (with the value: {mainPreview})");
                    }
                    else if (line.StartsWith("PlaygroundPath="))
                    {
                        string relativePath = line.Substring("PlaygroundPath=".Length);
                        BackgroundText.Text = Path.Combine(folderPath, relativePath);
                        mainImg = BackgroundText.Text;

                        Logger.Info($"Loaded setting 'PlaygroundPath' for Main Window (with the value: {BackgroundText.Text})");
                    }
                    else if (line.StartsWith("TopOverlayPath="))
                    {
                        string relativePath = line.Substring("TopOverlayPath=".Length);
                        TopOverlayText.Text = Path.Combine(folderPath, relativePath);

                        Logger.Info($"Loaded setting 'TopOverlayPath' for Main Window (with the value: {TopOverlayText.Text})");
                    }
                    else if (line.StartsWith("BottomOverlayPath="))
                    {
                        string relativePath = line.Substring("BottomOverlayPath=".Length);
                        BottomOverlayText.Text = Path.Combine(folderPath, relativePath);

                        Logger.Info($"Loaded setting 'BottomOverlayPath' for Main Window (with the value: {BottomOverlayText.Text})");
                    }
                    else if (line.StartsWith("LeftOverlayPath="))
                    {
                        string relativePath = line.Substring("LeftOverlayPath=".Length);
                        LeftOverlayText.Text = Path.Combine(folderPath, relativePath);

                        Logger.Info($"Loaded setting 'LeftOverlayPath' for Main Window (with the value: {LeftOverlayText.Text})");
                    }
                    else if (line.StartsWith("RightOverlayPath="))
                    {
                        string relativePath = line.Substring("RightOverlayPath=".Length);
                        RightOverlayText.Text = Path.Combine(folderPath, relativePath);

                        Logger.Info($"Loaded setting 'RightOverlayPath' for Main Window (with the value: {RightOverlayText.Text})");
                    }
                }
            }
            if (File.Exists(mainImg))
            {
                if (File.Exists(mainPreview))
                    PreviewImage.Source = LoadBitmap(mainPreview);
                else
                {
                    Logger.Warn($"Preview image not found, using main image: {mainImg}");
                    PreviewImage.Source = LoadBitmap(mainImg);
                }
            }
            else
            {
                Logger.Error("Playground cannot load the main image");
                MessageBox.Show("Playground not found");
                PreviewImage.Source = null;
            }
        }
        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Reset settings? (can't be undo)", "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Logger.Info("Reseting settings...");
                settings = new EditorSettings();
                Logger.Info("Settings now standard");
                ApplySettingsToUI();
            }
        }

        private void OpenPlaygroundsFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "playgrounds");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void LaunchPlayground()
        {
            Logger.Info($"Launched the playground: {settings.SelectedPlayground}");
            PlaygroundWindow pg = new PlaygroundWindow(settings, ReturnFromPlayground, animationFrames);
            pg.Show();
            this.Hide();
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e) => SaveSettings();
        private void checkSkipEditor()
        {
            if (settings.StartDirectPlayground)
            {
                Logger.Info("Skipped Editor:");
                Logger.Info($"Start Playground direct after Application launch: {settings.StartDirectPlayground}");
                LaunchPlayground();
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchPlayground();
        }
        private void ReturnFromPlayground()
        {
            Logger.Info("Shortcut + X triggered");
            Logger.Info("Returned to Editor");
            this.Show();
        }

        private void RunApp_Click(object sender, RoutedEventArgs e) // running python script (disabled console (CreateNoWindow = true))
        {
            if (sender is not Button btn) return;
            string entryPath = btn.Tag as string;
            if (!File.Exists(entryPath))
            {
                MessageBox.Show("entry.py not found!");
                Logger.Error("entry.py not found in " + entryPath);
                return;
            }

            string appName = Path.GetFileName(Path.GetDirectoryName(entryPath));
            string safeName = "Output_" + Regex.Replace(appName, @"[^\w]", ""); // safename for safe start (without it crash or im dumb because last time it worked)
            TextBox outputBox = FindName(safeName) as TextBox;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{entryPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                Process proc = new Process { StartInfo = psi };
                proc.OutputDataReceived += (s, args) =>
                {
                    if (args.Data != null)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show(args.Data + "\n"));
                    }
                };
                proc.ErrorDataReceived += (s, args) =>
                {
                    if (args.Data != null)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show("[ERROR] " + args.Data + "\n"));
                    }
                };

                Logger.Info("Running app: " + appName + " | Path: " + entryPath);

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show("[EXCEPTION] " + ex.Message + "\n");
                Logger.Error("[EXCEPTION] " + ex.Message + "\n");
            }
        }

        private void DragBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void EditorWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.X)
            {
                ReturnFromPlayground();
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.R)
            {
                Logger.Info("Shortcut + R triggered");
                RunButton_Click(null, null);
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.S)
            {
                Logger.Info("Shortcut + S triggered");
                SaveSettings();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Logger.Info("Shutting down...");
            base.OnClosed(e);
            animTimer?.Stop();
            Application.Current.Shutdown();
        }
    }
}