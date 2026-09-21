using System.Windows;

namespace SideScreen.UI.Localization;

/// <summary>Leitura de strings do dicionário ativo (com fallback para a chave).</summary>
public static class Loc
{
    public static string Get(string key) =>
        Application.Current?.TryFindResource(key) as string ?? key;
}

/// <summary>Troca de tema ao vivo (dicionário índice 0). Padrão: Dark.</summary>
public static class ThemeService
{
    public const string Dark = "Dark";
    public const string Light = "Light";

    public static string Current { get; private set; } = Dark;

    public static void Apply(string? theme)
    {
        var name = Normalize(theme);
        Current = name;
        Swap(0, $"Theme.{name}.xaml");
    }

    public static string Normalize(string? theme) =>
        string.Equals(theme, Light, StringComparison.OrdinalIgnoreCase) ? Light : Dark;

    private static void Swap(int index, string file)
    {
        var app = Application.Current;
        if (app is null) return;
        var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/MediaControl;component/{file}") };
        if (app.Resources.MergedDictionaries.Count > index)
            app.Resources.MergedDictionaries[index] = dict;
        else
            app.Resources.MergedDictionaries.Add(dict);
    }
}

/// <summary>Troca de idioma ao vivo (dicionário índice 1). Padrão: en.</summary>
public static class LanguageService
{
    public const string English = "en";
    public const string Portuguese = "pt-BR";

    public static string Current { get; private set; } = English;

    public static void Apply(string? lang)
    {
        var name = Normalize(lang);
        Current = name;
        var app = Application.Current;
        if (app is null) return;
        var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/MediaControl;component/Localization/Strings.{name}.xaml") };
        if (app.Resources.MergedDictionaries.Count > 1)
            app.Resources.MergedDictionaries[1] = dict;
        else
            app.Resources.MergedDictionaries.Add(dict);
    }

    public static string Normalize(string? lang) =>
        string.Equals(lang, Portuguese, StringComparison.OrdinalIgnoreCase) ? Portuguese : English;
}
