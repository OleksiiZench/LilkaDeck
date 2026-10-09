using System;
using System.Threading.Tasks;
using System.Windows.Input;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Mvvm;
using LilkaDeckApp.Profiles;
using LilkaDeckApp.Sync;

namespace LilkaDeckApp.ViewModels;

/// <summary>
/// The profile screen: which profile is open, its name and color, and the commands to move between,
/// add and delete profiles. Edits made by the user are announced by events; loading a profile is not an edit.
/// </summary>
public sealed class ProfileViewModel : ObservableObject, IOpenProfile
{
    private readonly IProfileDevice _device;
    private readonly ProfileLoader _loader;
    private readonly ActivityViewModel _activity;
    private readonly ConnectionViewModel _connection;
    private readonly Action<Exception> _onError;
    private readonly ProfileBrowser _browser = new();
    private readonly AsyncRelayCommand _previous;
    private readonly AsyncRelayCommand _next;
    private readonly AsyncRelayCommand _add;
    private readonly AsyncRelayCommand _delete;

    private string _title = "Профіль 0";
    private string _name = "Main Deck";
    private string _activeColor = "#00FFFF";
    private bool _isLoading;
    private bool _isBusy;
    private bool _isShowing;

    public ProfileViewModel(
        IProfileDevice device, ProfileLoader loader, ActivityViewModel activity, ConnectionViewModel connection, Action<Exception> onError)
    {
        _device = device;
        _loader = loader;
        _activity = activity;
        _connection = connection;
        _onError = onError;

        _previous = new AsyncRelayCommand(() => MoveAsync(_browser.MovePrevious), CanBrowse, onError);
        _next = new AsyncRelayCommand(() => MoveAsync(_browser.MoveNext), CanBrowse, onError);
        _add = new AsyncRelayCommand(AddAsync, CanAdd, onError);
        _delete = new AsyncRelayCommand(DeleteAsync, CanDelete, onError);

        _connection.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConnectionViewModel.IsOnline)) RaiseCommandStates();
        };
    }

    public event Action? NameEdited;
    public event Action<string>? ColorEdited;

    /// <summary>Raised when loading a profile ends. The document is null when it could not be read.</summary>
    public event Action<int, ProfileDocument?>? Loaded;

    public event Action? Removed;

    public ICommand PreviousCommand => _previous;
    public ICommand NextCommand => _next;
    public ICommand AddCommand => _add;
    public ICommand DeleteCommand => _delete;

    public int? CurrentProfileId => _browser.CurrentId;
    public bool HasProfiles => _browser.HasProfiles;

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (!SetProperty(ref _name, value) || _isShowing || _isLoading) return;

            UpdateTitle();
            NameEdited?.Invoke();
        }
    }

    /// <summary>"#RRGGBB".</summary>
    public string ActiveColor
    {
        get => _activeColor;
        set
        {
            if (!SetProperty(ref _activeColor, value) || _isShowing || _isLoading) return;

            ColorEdited?.Invoke(value);
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value)) RaiseCommandStates();
        }
    }

    /// <summary>Reads the list of profiles from the device and opens the preferred one, or the first.</summary>
    public async Task RefreshAsync(int preferredProfileId = 0)
    {
        var ids = await _device.GetProfileIdsAsync();
        _browser.SetProfiles(ids, preferredProfileId);
        RaiseCommandStates();

        if (_browser.HasProfiles) await LoadCurrentAsync();
        else Title = "Профілі відсутні";
    }

    private bool IsIdle => _connection.IsOnline && !_isLoading && !_isBusy;

    private bool CanBrowse() => IsIdle && _browser.HasProfiles;

    private bool CanAdd() => IsIdle;

    private bool CanDelete() => IsIdle && _browser.CanDelete;

    private async Task MoveAsync(Func<int?> move)
    {
        move();
        RaiseCommandStates();
        await LoadCurrentAsync();
    }

    private async Task LoadCurrentAsync()
    {
        if (_browser.CurrentId is not int profileId) return;

        _activity.Info($"Завантаження Профілю {profileId}...");
        IsLoading = true;
        Title = $"Профіль {profileId}";

        ProfileDocument? document = null;
        try
        {
            document = await _loader.LoadAsync(
                profileId,
                onConfigLoaded: config => ShowHeader(profileId, config),
                onIconDownloading: icon => _activity.Info($"Завантаження {icon}..."));
            _activity.Info("Готово до редагування");
        }
        catch (Exception ex)
        {
            _onError(ex);
        }
        finally
        {
            IsLoading = false;
            Loaded?.Invoke(profileId, document);
        }
    }

    private void ShowHeader(int profileId, ProfileDocument document)
    {
        _isShowing = true;
        try
        {
            Name = document.Name;
            ActiveColor = document.ActiveColor;
        }
        finally
        {
            _isShowing = false;
        }
        Title = $"Профіль {profileId}: {document.Name}";
    }

    private void UpdateTitle()
    {
        if (_browser.CurrentId is int profileId) Title = $"Профіль {profileId}: {_name}";
    }

    private async Task AddAsync()
    {
        _activity.Info("Створення нового профілю...");
        await RunBusyAsync(async () =>
        {
            int? newId = await _device.CreateProfileAsync();
            if (newId.HasValue)
            {
                _activity.Info($"Профіль {newId.Value} успішно створено!");
                await RefreshAsync(newId.Value);
            }
            else
            {
                _activity.Error("Помилка: Лілка не відповіла на створення профілю.");
            }
        });
    }

    private async Task DeleteAsync()
    {
        if (_browser.CurrentId is not int profileId) return;

        _activity.Info($"Видалення профілю {profileId}...");
        await RunBusyAsync(async () =>
        {
            if (await _device.DeleteProfileAsync(profileId))
            {
                _activity.Info("Профіль успішно видалено!");
                Removed?.Invoke();
                await RefreshAsync();
            }
            else
            {
                _activity.Error("Помилка при видаленні профілю.");
            }
        });
    }

    private async Task RunBusyAsync(Func<Task> work)
    {
        _isBusy = true;
        RaiseCommandStates();
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            _onError(ex);
        }
        finally
        {
            _isBusy = false;
            RaiseCommandStates();
        }
    }

    private void RaiseCommandStates()
    {
        _previous.RaiseCanExecuteChanged();
        _next.RaiseCanExecuteChanged();
        _add.RaiseCanExecuteChanged();
        _delete.RaiseCanExecuteChanged();
    }
}
