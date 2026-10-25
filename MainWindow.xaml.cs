using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using MediaControl.Models;
using MediaControl.Services;
using MediaControl.Localization;
using Forms = System.Windows.Forms;

namespace MediaControl;

public partial class MainWindow : Window
{
    private readonly bool _preview;
    private AppSettings _settings;
    private AppSettings _pending;
    private GlobalHotkeys? _hotkeys;
    private ForegroundProgramMonitor? _foregroundMonitor;
    private readonly DispatcherTimer _statusTimer;
    private string? _capturingAction;
    private bool _settingVolume;
    private bool _seeking;
    private bool _refreshing;
    private DateTime _messageUntil;
    private int _volumeBeforeMute = 65;
    private long _durationMs;
    private string? _lastScopeError;
    private Forms.NotifyIcon? _trayIcon;
    private Forms.ContextMenuStrip? _trayMenu;
    private System.Drawing.Icon? _trayImage;

    public MainWindow(bool preview)
    {
        _preview = preview;
        _settings = preview ? AppSettings.CreateDefault() : SettingsStore.Load();
        if (preview)
        {
            _settings.AllowedExecutables.Add(@"C:\Games\ExampleGame.exe");
            _settings.OnlyListedPrograms = true;
        }
        _pending = _settings.Copy();
        UiText.SetLanguage(_settings.Language);
        InitializeComponent();
        UiText.ApplyStaticText(this);
        RenderBindings();
        RenderProgramSettings();
        RenderLanguageButtons();
        RenderPauseState();
        TrackTitle.Text = UiText.T("Faixa atual");
        TrackTitle.ToolTip = TrackTitle.Text;
        PlaybackBadge.Text = UiText.T("TOCANDO");
        PlayPauseCaption.Text = UiText.T("PAUSAR");
        HintText.Text = UiText.T("Clique na tecla para alterar");

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _statusTimer.Tick += async (_, _) => await RefreshStatusAsync();
        SourceInitialized += MainWindow_SourceInitialized;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        if (_preview)
            return;

        _hotkeys = new GlobalHotkeys(this, ExecuteHotkeyAction, ToggleHotkeys,
            scopeEnabled: !_settings.OnlyListedPrograms && !_settings.HotkeysPaused);
        if (!_hotkeys.TryApply(_settings.Bindings, out string error))
        {
            ShowMessage(error);
            HintText.Text = UiText.T("Atalhos indisponíveis: abra Atalhos");
        }
        _foregroundMonitor = new ForegroundProgramMonitor(this, UpdateHotkeyScope);
        _statusTimer.Start();
        await RefreshStatusAsync();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _statusTimer.Stop();
        _foregroundMonitor?.Dispose();
        _hotkeys?.Dispose();
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        _trayMenu?.Dispose();
        _trayImage?.Dispose();
    }

    private async Task RefreshStatusAsync()
    {
        if (_preview || _refreshing)
            return;
        _refreshing = true;
        try
        {
            PlayerSnapshot snapshot = await Task.Run(PotPlayerController.ReadSnapshot);
            bool messageVisible = DateTime.UtcNow < _messageUntil;
            if (!messageVisible)
            {
                PlaybackBadge.Text = UiText.T(snapshot.State);
                PlaybackBadge.ToolTip = null;
                TrackTitle.Text = UiText.T(snapshot.Title);
                TrackTitle.ToolTip = TrackTitle.Text;
            }

            bool playing = snapshot.State == "TOCANDO";
            PlayPauseIcon.Source = LoadIcon(playing ? "pause" : "play");
            PlayPauseCaption.Text = UiText.T(playing ? "PAUSAR" : "TOCAR");

            if (snapshot.Volume is int volume)
            {
                if (volume > 0)
                    _volumeBeforeMute = volume;
                SetVolumeDisplay(volume);
            }
            if (snapshot.DurationMs is long duration and > 0)
            {
                _durationMs = duration;
                long position = Math.Clamp(snapshot.PositionMs ?? 0, 0, duration);
                if (!_seeking)
                {
                    ProgressSlider.Value = 100d * position / duration;
                    CurrentTimeText.Text = FormatTime(position);
                }
                TotalTimeText.Text = FormatTime(duration);
            }
            else
            {
                _durationMs = 0;
                if (!_seeking)
                {
                    ProgressSlider.Value = 0;
                    CurrentTimeText.Text = "00:00";
                    TotalTimeText.Text = "00:00";
                }
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private static string FormatTime(long milliseconds)
    {
        var time = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }

    private static BitmapImage LoadIcon(string name) => new(new Uri($"pack://application:,,,/Assets/Lucide/png/{name}.png", UriKind.Absolute));

    private void ShowMessage(string message, bool error = true)
    {
        TrackTitle.Text = message;
        TrackTitle.ToolTip = message;
        PlaybackBadge.Text = UiText.T(error ? "AVISO" : "SALVO");
        PlaybackBadge.ToolTip = message;
        _messageUntil = DateTime.UtcNow.AddSeconds(5);
    }

    private void ExecuteHotkeyAction(string action)
    {
        if (_settings.HotkeysPaused ||
            (_settings.OnlyListedPrograms && !IsListed(ForegroundProgramMonitor.GetForegroundExecutablePath())))
            return;
        ExecuteAction(action);
    }

    private void ExecuteAction(string action)
    {
        if (_preview)
            return;
        if (!PotPlayerController.TryExecute(action, out string error))
            ShowMessage(error);
        else
            _ = RefreshAfterActionAsync();
    }

    private async Task RefreshAfterActionAsync()
    {
        await Task.Delay(180);
        await RefreshStatusAsync();
    }

    private void SetVolumeDisplay(int volume)
    {
        volume = Math.Clamp(volume, 0, 100);
        _settingVolume = true;
        VolumeSlider.Value = volume;
        VolumePageSlider.Value = volume;
        _settingVolume = false;
        VolumeValueText.Text = $"{volume}%";
        VolumePageValueText.Text = $"{volume}%";
        PlaybackVolumeIcon.Source = LoadIcon(volume == 0 ? "volume-x" : "volume-2");
        VolumePageMuteIcon.Source = LoadIcon(volume == 0 ? "volume-2" : "volume-x");
    }

    private void RenderBindings()
    {
        (string id, Button button)[] items =
        [
            (ControlActions.Next, NextBinding),
            (ControlActions.PlayPause, PauseBinding),
            (ControlActions.VolumeUp, VolumeUpBinding),
            (ControlActions.VolumeDown, VolumeDownBinding),
            (ControlActions.SeekBackward, SeekBackBinding),
            (ControlActions.SeekForward, SeekForwardBinding),
            (ControlActions.PauseHotkeys, PauseHotkeysBinding),
        ];
        foreach (var (id, button) in items)
        {
            string display = _pending.Bindings[id].Display;
            button.Content = display;
            button.ToolTip = display;
        }
        NextTrackButtonLabel.Text = $"{UiText.T("Próxima faixa")}  ·  {_pending.Bindings[ControlActions.Next].Display}";
        VolumeHotkeyHint.Text = $"{_pending.Bindings[ControlActions.VolumeDown].Display} / {_pending.Bindings[ControlActions.VolumeUp].Display} {UiText.T("ajustam o volume")}";
    }

    private bool IsListed(string? executablePath) => executablePath is not null
        && (System.IO.Path.IsPathFullyQualified(executablePath)
            ? _settings.AllowedExecutables.Contains(executablePath, StringComparer.OrdinalIgnoreCase)
            : _settings.AllowedExecutables.Any(path => string.Equals(
                System.IO.Path.GetFileNameWithoutExtension(path),
                System.IO.Path.GetFileNameWithoutExtension(executablePath),
                StringComparison.OrdinalIgnoreCase)));

    private void UpdateHotkeyScope(string? foregroundPath)
    {
        bool inScope = !_settings.OnlyListedPrograms || IsListed(foregroundPath);
        bool enabled = !_settings.HotkeysPaused && inScope;
        if (_hotkeys is not null && !_hotkeys.SetScopeEnabled(enabled, out string error))
        {
            ProgramStatusText.Text = UiText.T("Falha ao registrar atalhos neste programa");
            ProgramStatusText.ToolTip = error;
            if (error != _lastScopeError)
                ShowMessage(error);
            _lastScopeError = error;
            return;
        }
        _lastScopeError = null;
        ProgramStatusText.ToolTip = null;
        ProgramStatusText.Text = _settings.HotkeysPaused
            ? string.Format(UiText.T("Atalhos pausados · {0} para reativar"), PauseHotkeyDisplay)
            : UiText.T(!_settings.OnlyListedPrograms
            ? "Atalhos ativos em todos os programas"
            : _settings.AllowedExecutables.Count == 0
                ? "Lista vazia: atalhos desativados"
                : inScope ? "Atalhos ativos neste programa" : "Fora da lista: teclas livres");
    }

    private void RenderProgramSettings()
    {
        AllProgramsButton.Background = new SolidColorBrush(
            _settings.OnlyListedPrograms ? Color.FromRgb(55, 56, 59) : Color.FromRgb(7, 139, 234));
        OnlyListButton.Background = new SolidColorBrush(
            _settings.OnlyListedPrograms ? Color.FromRgb(7, 139, 234) : Color.FromRgb(55, 56, 59));
        ProgramList.Items.Clear();
        foreach (string path in _settings.AllowedExecutables)
        {
            ProgramList.Items.Add(new ListBoxItem
            {
                Content = System.IO.Path.GetFileName(path),
                Tag = path,
                ToolTip = path,
            });
        }
        ProgramEmptyText.Visibility = _settings.AllowedExecutables.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
        UpdateHotkeyScope(ForegroundProgramMonitor.GetForegroundExecutablePath());
    }

    private void SaveProgramSettings(Action<AppSettings> change)
    {
        var updated = _settings.Copy();
        change(updated);
        try
        {
            SettingsStore.Save(updated);
        }
        catch (Exception ex)
        {
            ShowMessage($"{UiText.T("Falha ao salvar programas: ")}{ex.Message}");
            return;
        }
        _settings = updated;
        _pending.OnlyListedPrograms = updated.OnlyListedPrograms;
        _pending.AllowedExecutables = [.. updated.AllowedExecutables];
        RenderProgramSettings();
    }

    private void AddProgram(string path)
    {
        path = System.IO.Path.GetFullPath(path);
        if (_settings.AllowedExecutables.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            if (!_settings.OnlyListedPrograms)
                SaveProgramSettings(settings => settings.OnlyListedPrograms = true);
            else
                ProgramStatusText.Text = UiText.T("Esse programa já está na lista");
            return;
        }
        SaveProgramSettings(settings =>
        {
            settings.AllowedExecutables.Add(path);
            settings.OnlyListedPrograms = true;
        });
    }

    private void AllPrograms_Click(object sender, RoutedEventArgs e) =>
        SaveProgramSettings(settings => settings.OnlyListedPrograms = false);

    private void OnlyList_Click(object sender, RoutedEventArgs e) =>
        SaveProgramSettings(settings => settings.OnlyListedPrograms = true);

    private void ChooseExecutable_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = UiText.T("Escolher programa para os atalhos"),
            Filter = UiText.T("Programas (*.exe;*.bin)|*.exe;*.bin|Todos os arquivos (*.*)|*.*"),
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) == true)
            AddProgram(dialog.FileName);
    }

    private void AddOpenWindow_Click(object sender, RoutedEventArgs e)
    {
        var windows = ForegroundProgramMonitor.ListOpenWindows();
        if (windows.Count == 0)
        {
            ProgramStatusText.Text = UiText.T("Nenhuma janela de programa encontrada");
            return;
        }
        var picker = new OpenWindowPicker(windows) { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedWindow is { } selected)
            AddProgram(selected.ExecutablePath);
    }

    private void RemoveProgram_Click(object sender, RoutedEventArgs e)
    {
        if (ProgramList.SelectedItem is not ListBoxItem { Tag: string path })
        {
            ProgramStatusText.Text = UiText.T("Selecione um programa da lista");
            return;
        }
        SaveProgramSettings(settings => settings.AllowedExecutables.RemoveAll(
            item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase)));
    }

    private void SetPage(FrameworkElement page, Button nav)
    {
        CancelCapture();
        (FrameworkElement page, Button nav)[] pages =
        [
            (PlaybackPage, NavPlayback), (TracksPage, NavTracks),
            (VolumePage, NavVolume), (ProgramsPage, NavPrograms), (HotkeyPage, NavHotkeys),
            (GeneralPage, NavGeneral),
        ];
        foreach (var item in pages)
        {
            bool selected = ReferenceEquals(item.page, page);
            item.page.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            item.nav.Background = selected
                ? new SolidColorBrush(Color.FromRgb(80, 81, 84))
                : Brushes.Transparent;
        }
    }

    internal void ShowPreviewPage(string page)
    {
        switch (page.ToLowerInvariant())
        {
            case "faixas": SetPage(TracksPage, NavTracks); break;
            case "volume": SetPage(VolumePage, NavVolume); break;
            case "programas": SetPage(ProgramsPage, NavPrograms); break;
            case "atalhos": SetPage(HotkeyPage, NavHotkeys); break;
            case "geral": SetPage(GeneralPage, NavGeneral); break;
            default: SetPage(PlaybackPage, NavPlayback); break;
        }
    }

    internal void SetPreviewLanguage(string language)
    {
        if (_preview)
            ChangeLanguage(language);
    }

    internal void SetPreviewPaused(bool paused)
    {
        if (!_preview)
            return;
        _settings.HotkeysPaused = paused;
        RenderPauseState();
        UpdateHotkeyScope(null);
    }

    private void NavPlayback_Click(object sender, RoutedEventArgs e) => SetPage(PlaybackPage, NavPlayback);
    private void NavTracks_Click(object sender, RoutedEventArgs e) => SetPage(TracksPage, NavTracks);
    private void NavVolume_Click(object sender, RoutedEventArgs e) => SetPage(VolumePage, NavVolume);
    private void NavPrograms_Click(object sender, RoutedEventArgs e) => SetPage(ProgramsPage, NavPrograms);
    private void NavHotkeys_Click(object sender, RoutedEventArgs e) => SetPage(HotkeyPage, NavHotkeys);
    private void NavGeneral_Click(object sender, RoutedEventArgs e) => SetPage(GeneralPage, NavGeneral);

    private void English_Click(object sender, RoutedEventArgs e) => ChangeLanguage("en");
    private void Portuguese_Click(object sender, RoutedEventArgs e) => ChangeLanguage("pt-BR");

    private void ChangeLanguage(string language)
    {
        if (_settings.Language == language)
            return;
        var updated = _settings.Copy();
        updated.Language = language;
        if (!_preview)
        {
            try { SettingsStore.Save(updated); }
            catch (Exception ex)
            {
                ShowMessage($"{UiText.T("Não foi possível salvar o idioma: ")}{ex.Message}");
                return;
            }
        }
        _settings = updated;
        _pending.Language = language;
        UiText.SetLanguage(language);
        UiText.ApplyStaticText(this);
        if (_trayMenu is { Items.Count: >= 3 })
        {
            _trayMenu.Items[0].Text = UiText.T("Abrir MediaControl");
            _trayMenu.Items[2].Text = UiText.T("Sair");
        }
        RenderLanguageButtons();
        RenderPauseState();
        RenderBindings();
        RenderProgramSettings();
        HintText.Text = UiText.T("Clique na tecla para alterar");
        if (!_preview)
            _ = RefreshStatusAsync();
    }

    private void RenderLanguageButtons()
    {
        EnglishButton.Background = new SolidColorBrush(_settings.Language == "en"
            ? Color.FromRgb(7, 139, 234) : Color.FromRgb(55, 56, 59));
        PortugueseButton.Background = new SolidColorBrush(_settings.Language == "pt-BR"
            ? Color.FromRgb(7, 139, 234) : Color.FromRgb(55, 56, 59));
    }

    private void PauseHotkeys_Click(object sender, RoutedEventArgs e) => ToggleHotkeys();

    private void ToggleHotkeys()
    {
        var updated = _settings.Copy();
        updated.HotkeysPaused = !updated.HotkeysPaused;
        _settings = updated;
        _pending.HotkeysPaused = updated.HotkeysPaused;
        UpdateHotkeyScope(ForegroundProgramMonitor.GetForegroundExecutablePath());
        RenderPauseState();
        if (_preview)
            return;
        try
        {
            SettingsStore.Save(updated);
        }
        catch (Exception ex)
        {
            ShowMessage($"{UiText.T("Não foi possível salvar o estado dos atalhos: ")}{ex.Message}");
        }
    }

    private void RenderPauseState()
    {
        bool paused = _settings.HotkeysPaused;
        string hotkey = PauseHotkeyDisplay;
        PauseHotkeysButton.Content = UiText.T(paused ? "PAUSADO" : "ATIVO");
        PauseHotkeysButton.Background = new SolidColorBrush(paused
            ? Color.FromRgb(143, 91, 29) : Color.FromRgb(18, 102, 159));
        PauseHotkeysButton.ToolTip = string.Format(
            UiText.T(paused ? "{0} · Reativar atalhos" : "{0} · Pausar atalhos"), hotkey);
        PauseHotkeyHelpText.Text = string.Format(
            UiText.T("{0} pausa ou reativa os atalhos em qualquer programa."), hotkey);
        if (_trayMenu is { Items.Count: >= 3 })
            _trayMenu.Items[1].Text = string.Format(
                UiText.T(paused ? "Reativar atalhos ({0})" : "Pausar atalhos ({0})"), hotkey);
        if (_trayIcon is not null)
            _trayIcon.Text = paused ? $"MediaControl · {UiText.T("PAUSADO")}" : "MediaControl";
    }

    private string PauseHotkeyDisplay => _settings.Bindings[ControlActions.PauseHotkeys].Display;
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        if (_preview)
            return;
        EnsureTrayIcon();
        Hide();
        ShowInTaskbar = false;
    }

    private void EnsureTrayIcon()
    {
        if (_trayIcon is not null)
            return;

        try
        {
            if (Environment.ProcessPath is { } executable)
                _trayImage = System.Drawing.Icon.ExtractAssociatedIcon(executable);
        }
        catch (Exception)
        {
            // Usa o ícone padrão se o executável não puder fornecer seu ícone.
        }
        _trayImage ??= (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
        _trayMenu = new Forms.ContextMenuStrip();
        _trayMenu.Items.Add(UiText.T("Abrir MediaControl"), null, (_, _) => Dispatcher.BeginInvoke(RestoreFromTray));
        _trayMenu.Items.Add(string.Format(
                UiText.T(_settings.HotkeysPaused ? "Reativar atalhos ({0})" : "Pausar atalhos ({0})"),
                PauseHotkeyDisplay),
            null, (_, _) => Dispatcher.BeginInvoke(ToggleHotkeys));
        _trayMenu.Items.Add(UiText.T("Sair"), null, (_, _) => Dispatcher.BeginInvoke(Close));
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayImage,
            Text = _settings.HotkeysPaused ? $"MediaControl · {UiText.T("PAUSADO")}" : "MediaControl",
            ContextMenuStrip = _trayMenu,
            Visible = true,
        };
        _trayIcon.MouseClick += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left)
                Dispatcher.BeginInvoke(RestoreFromTray);
        };
    }

    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        for (DependencyObject? node = e.OriginalSource as DependencyObject;
             node is not null && node != sender;
             node = VisualTreeHelper.GetParent(node))
        {
            if (node is Button or Slider or Thumb)
                return;
        }
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e) => ExecuteAction(ControlActions.PlayPause);
    private void Previous_Click(object sender, RoutedEventArgs e) => ExecuteAction(ControlActions.Previous);
    private void Next_Click(object sender, RoutedEventArgs e) => ExecuteAction(ControlActions.Next);
    private void SeekBackward_Click(object sender, RoutedEventArgs e) => ExecuteAction(ControlActions.SeekBackward);
    private void SeekForward_Click(object sender, RoutedEventArgs e) => ExecuteAction(ControlActions.SeekForward);
    private void VolumeDown_Click(object sender, RoutedEventArgs e) => ExecuteAction(ControlActions.VolumeDown);
    private void VolumeUp_Click(object sender, RoutedEventArgs e) => ExecuteAction(ControlActions.VolumeUp);

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        if (_preview)
            return;
        int current = (int)Math.Round(VolumeSlider.Value);
        int target;
        if (current > 0)
        {
            _volumeBeforeMute = current;
            target = 0;
        }
        else
        {
            target = _volumeBeforeMute;
        }
        SetVolumeOnPlayer(target);
    }

    private void SetVolumeOnPlayer(int volume)
    {
        if (!PotPlayerController.TrySetVolume(volume, out string error))
            ShowMessage(error);
        else
            SetVolumeDisplay(volume);
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_preview || _settingVolume || !IsLoaded)
            return;
        int volume = (int)Math.Round(e.NewValue);
        if (volume > 0)
            _volumeBeforeMute = volume;
        SetVolumeOnPlayer(volume);
    }

    private void ProgressSlider_MouseDown(object sender, MouseButtonEventArgs e) => _seeking = true;

    private void ProgressSlider_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_seeking)
            return;
        _seeking = false;
        if (_preview || _durationMs <= 0)
            return;
        long target = (long)(_durationMs * ProgressSlider.Value / 100d);
        if (!PotPlayerController.TrySetPosition(target, out string error))
            ShowMessage(error);
        else
        {
            CurrentTimeText.Text = FormatTime(target);
            _ = RefreshAfterActionAsync();
        }
    }

    private void Binding_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;
        string? action = button.Name switch
        {
            nameof(NextBinding) => ControlActions.Next,
            nameof(PauseBinding) => ControlActions.PlayPause,
            nameof(VolumeUpBinding) => ControlActions.VolumeUp,
            nameof(VolumeDownBinding) => ControlActions.VolumeDown,
            nameof(SeekBackBinding) => ControlActions.SeekBackward,
            nameof(SeekForwardBinding) => ControlActions.SeekForward,
            nameof(PauseHotkeysBinding) => ControlActions.PauseHotkeys,
            _ => null,
        };
        if (action is null)
            return;
        CancelCapture();
        _capturingAction = action;
        _hotkeys?.Suspend();
        button.Content = UiText.T("Pressione...");
        HintText.Text = UiText.T("Pressione uma tecla · Esc cancela");
        button.Focus();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_capturingAction is null)
            return;
        e.Handled = true;
        if (e.Key == Key.Escape)
        {
            CancelCapture();
            return;
        }
        HotkeyGesture? gesture = HotkeyGesture.FromKeyEvent(e);
        if (gesture is null || !gesture.IsValid)
            return;
        _pending.Bindings[_capturingAction] = gesture;
        _capturingAction = null;
        RenderBindings();
        HintText.Text = UiText.T("Clique em Aplicar para salvar");
        if (_hotkeys is not null && !_hotkeys.Resume(out string error))
            ShowMessage(error);
    }

    private void Window_Deactivated(object? sender, EventArgs e) => CancelCapture();

    private void CancelCapture()
    {
        if (_capturingAction is null)
            return;
        _capturingAction = null;
        RenderBindings();
        HintText.Text = UiText.T("Clique na tecla para alterar");
        if (_hotkeys is not null && !_hotkeys.Resume(out string error))
            ShowMessage(error);
    }

    private void Defaults_Click(object sender, RoutedEventArgs e)
    {
        CancelCapture();
        _pending.Bindings = AppSettings.CreateDefault().Bindings;
        RenderBindings();
        HintText.Text = UiText.T("Clique em Aplicar para salvar");
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        CancelCapture();
        if (_preview)
            return;
        string error = UiText.T("Atalhos indisponíveis.");
        if (_hotkeys is null || !_hotkeys.TryApply(_pending.Bindings, out error))
        {
            ShowMessage(error);
            HintText.Text = UiText.T("Falha: veja o aviso no painel principal");
            return;
        }
        try
        {
            SettingsStore.Save(_pending);
            _settings = _pending.Copy();
            RenderPauseState();
            UpdateHotkeyScope(ForegroundProgramMonitor.GetForegroundExecutablePath());
            HintText.Text = UiText.T("Atalhos salvos");
            ShowMessage(UiText.T("Atalhos salvos"), error: false);
        }
        catch (Exception ex)
        {
            _hotkeys.TryApply(_settings.Bindings, out _);
            ShowMessage($"{UiText.T("Falha ao salvar: ")}{ex.Message}");
            HintText.Text = UiText.T("Falha ao salvar");
        }
    }
}
