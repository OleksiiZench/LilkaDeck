using Avalonia.Controls;
using Avalonia.Input;
using LilkaDeckApp.Services;


namespace LilkaDeckApp;

public partial class MainWindow : Window
{
    private readonly LilkaCommunicationService _comService;
    private readonly ProfileDataService _profileData;

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
}
