using SideScreen.Core.Config;
using SideScreen.UI.Localization;

namespace SideScreen.UI.ViewModels;

public sealed record ThemeOption(string Id, string Label);
public sealed record LanguageOption(string Id, string Label);

/// <summary>Settings — aparência e idioma ao vivo + infos. Persistência via config central.</summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly Action<Action<AppConfig>> _update;
    private readonly Action<string> _onLanguage;
    private string _appearance;
    private string _language;

    public SettingsViewModel(Func<AppConfig> getConfig, Action<Action<AppConfig>> update, Action<string> onLanguage)
    {
        _update = update;
        _onLanguage = onLanguage;
        var cfg = getConfig();
        _appearance = ThemeService.Normalize(cfg.Appearance);
        _language = LanguageService.Normalize(cfg.Language);
    }

    public string Version => "MediaControl 1.0";
    public string Runtime => $".NET {Environment.Version} + WPF";
    public string ConfigPath => @"%AppData%\SideScreen\config.json";

    public List<ThemeOption> Themes { get; } =
    [
        new ThemeOption(ThemeService.Dark, Loc.Get("S_ThemeDark")),
        new ThemeOption(ThemeService.Light, Loc.Get("S_ThemeLight")),
    ];

    public List<LanguageOption> Languages { get; } =
    [
        new LanguageOption(LanguageService.English, Loc.Get("S_LangEn")),
        new LanguageOption(LanguageService.Portuguese, Loc.Get("S_LangPt")),
    ];

    public string Appearance
    {
        get => _appearance;
        set
        {
            var norm = ThemeService.Normalize(value);
            if (Set(ref _appearance, norm))
            {
                try
                {
                    _update(c => c.Appearance = norm);
                    ThemeService.Apply(norm);
                }
                catch { }
            }
        }
    }

    public string Language
    {
        get => _language;
        set
        {
            var norm = LanguageService.Normalize(value);
            if (Set(ref _language, norm))
            {
                try
                {
                    _update(c => c.Language = norm);
                    _onLanguage(norm);
                }
                catch { }
            }
        }
    }
}
