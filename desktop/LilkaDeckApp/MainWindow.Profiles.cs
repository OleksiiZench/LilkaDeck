using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Profiles;

namespace LilkaDeckApp;

// Which profiles the device has, moving between them, loading one into the editor, adding and deleting.
public partial class MainWindow
{
    private readonly ProfileBrowser _profileBrowser = new();
    private ProfileLoader? _profileLoader;

    private ProfileLoader Loader => _profileLoader ??= new ProfileLoader(_comService, _profileData.Caches);

    private async Task RefreshProfileStateAsync(int targetProfileId = 0)
    {
        var ids = (await _comService.GetProfilesListAsync())
            .Select(text => int.TryParse(text, out int id) ? id : -1)
            .Where(id => id >= 0);
        _profileBrowser.SetProfiles(ids, targetProfileId);

        if (_profileBrowser.HasProfiles)
        {
            await LoadActiveProfileAsync();
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                ActiveProfileTitle.Text = "Профілі відсутні";
                DeleteProfileButton.IsEnabled = false;
            });
        }
    }

    private void OnPrevProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (!_profileBrowser.HasProfiles || _isLoadingProfile) return;

        _profileBrowser.MovePrevious();
        _ = LoadActiveProfileAsync();
    }

    private void OnNextProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (!_profileBrowser.HasProfiles || _isLoadingProfile) return;

        _profileBrowser.MoveNext();
        _ = LoadActiveProfileAsync();
    }

    private async Task LoadActiveProfileAsync()
    {
        if (_profileBrowser.CurrentId is not int profileId) return;

        AppLog($"Завантаження Профілю {profileId}...");
        _isLoadingProfile = true;

        Dispatcher.UIThread.Post(() =>
        {
            ActiveProfileTitle.Text = $"Профіль {profileId}";
            DeleteProfileButton.IsEnabled = _profileBrowser.CanDelete;
        });

        try
        {
            var document = await Loader.LoadAsync(
                profileId,
                onConfigLoaded: config => Dispatcher.UIThread.Post(() => ShowProfileHeader(profileId, config)),
                onIconDownloading: icon => AppLog($"Завантаження {icon}..."));

            if (document != null)
            {
                _profileData.ShowProfile(profileId, document);
                UpdateDeckVisuals();
            }
            AppLog("Готово до редагування");
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
        finally
        {
            _isLoadingProfile = false;

            Dispatcher.UIThread.Post(() =>
            {
                if (!string.IsNullOrEmpty(_currentSelectedPosition))
                {
                    OnDeckButtonClicked(new Avalonia.Controls.Button { Tag = _currentSelectedPosition }, new RoutedEventArgs());
                }
            });
        }
    }

    private void ShowProfileHeader(int profileId, ProfileDocument document)
    {
        ProfileNameTextBox.TextChanged -= OnProfileNameChanged;
        ProfileNameTextBox.Text = document.Name;
        ProfileNameTextBox.TextChanged += OnProfileNameChanged;

        ActiveProfileTitle.Text = $"Профіль {profileId}: {document.Name}";

        try
        {
            _lastColor = document.ActiveColor;
            ActiveColorPicker.Color = Color.Parse(document.ActiveColor);
        }
        catch (Exception)
        {
            // A color the editor cannot read leaves the previous one in place.
        }
    }

    private async void OnAddProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (!_comService.IsConnected || _isLoadingProfile) return;

        AppLog("Створення нового профілю...");
        AddProfileButton.IsEnabled = false;
        DeleteProfileButton.IsEnabled = false;

        try
        {
            int? newId = await _comService.CreateProfileAsync();
            if (newId.HasValue)
            {
                AppLog($"Профіль {newId.Value} успішно створено!");
                await RefreshProfileStateAsync(newId.Value);
            }
            else
            {
                AppLog("Помилка: Лілка не відповіла на створення профілю.", true);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
        finally
        {
            AddProfileButton.IsEnabled = true;
        }
    }

    private async void OnDeleteProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (!_comService.IsConnected || _isLoadingProfile || _profileBrowser.CurrentId is not int profileId) return;

        AppLog($"Видалення профілю {profileId}...");
        AddProfileButton.IsEnabled = false;
        DeleteProfileButton.IsEnabled = false;

        try
        {
            if (await _comService.DeleteProfileAsync(profileId))
            {
                AppLog("Профіль успішно видалено!");
                _profileData.ClearState();
                await RefreshProfileStateAsync();
            }
            else
            {
                AppLog("Помилка при видаленні профілю.", true);
                DeleteProfileButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);
            DeleteProfileButton.IsEnabled = true;
        }
        finally
        {
            AddProfileButton.IsEnabled = true;
        }
    }
}
