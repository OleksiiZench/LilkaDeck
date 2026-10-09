using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using System.Linq;
using System;
using System.IO;
using LilkaDeckApp.Domain;

using LilkaDeckApp.Services;


namespace LilkaDeckApp;

public partial class MainWindow : Window
{
    private readonly LilkaCommunicationService _comService;
    private readonly ProfileDataService _profileData;

    // Temporary: MainWindow.Icons.cs still works with the position as a string.
private string _currentSelectedPosition => _viewModel.Deck.SelectedPosition?.ToWireName() ?? "";

    public bool IsRealClose { get; set; } = false;

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnDrop);

        _profileData = new ProfileDataService();
        _comService = new LilkaCommunicationService();

        InitializeShell();

        _comService.OnConnected += HandleConnected;
        _comService.OnDisconnected += HandleDisconnected;
        _comService.OnExecuteRequested += HandleExecuteRequest;
        _comService.OnLogMessage += HandleLogMessage;
        _comService.OnError += HandleError;

        _comService.StartAutoScanner();
    }   

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!IsRealClose)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            base.OnClosing(e);
        }
    }

    // --- GALLERY LOGIC ---

    private void OnGalleryClicked(object? sender, RoutedEventArgs e)
    {
        GalleryWrapPanel.Children.Clear();

        try
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string galleryDir = Path.Combine(appDir, "assets", "standard_icons");

            if (!Directory.Exists(galleryDir))
            {
                Directory.CreateDirectory(galleryDir);
                AppLog("Створено папку для галереї: " + galleryDir);
                return;
            }

            var files = Directory.GetFiles(galleryDir)
                                 .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                                             f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                 .ToArray();

            if (files.Length == 0)
            {
                AppLog("Галерея порожня. Додайте картинки в папку Assets/StandardIcons.");
                return;
            }

            foreach (string file in files)
            {
                var bitmap = new Bitmap(file);
                
                var image = new Avalonia.Controls.Image 
                { 
                    Source = bitmap,
                    Stretch = Stretch.Uniform 
                };

                RenderOptions.SetBitmapInterpolationMode(image, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);

                var btn = new Button
                {
                    Content = image,
                    Width = 60,
                    Height = 60,
                    Margin = new Avalonia.Thickness(2),
                    Padding = new Avalonia.Thickness(6),
                    Tag = file,
                    Background = SolidColorBrush.Parse("#333")
                };

                btn.Click += OnGalleryIconSelected;
                
                GalleryWrapPanel.Children.Add(btn);
            }
        }
        catch (Exception ex)
        {
            AppLog($"Помилка завантаження галереї: {ex.Message}", true);
        }
    }
}
