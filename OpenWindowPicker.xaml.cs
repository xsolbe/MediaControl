using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MediaControl.Services;
using MediaControl.Localization;

namespace MediaControl;

public partial class OpenWindowPicker : Window
{
    private readonly ICollectionView _windows;

    internal WindowCandidate? SelectedWindow => WindowList.SelectedItem as WindowCandidate;

    internal OpenWindowPicker(IReadOnlyList<WindowCandidate> windows)
    {
        InitializeComponent();
        Title = UiText.T("Adicionar janela aberta");
        _windows = CollectionViewSource.GetDefaultView(windows.ToList());
        _windows.Filter = MatchesSearch;
        WindowList.ItemsSource = _windows;
        UiText.ApplyStaticText(this);
        UpdateResults();
        Loaded += (_, _) => SearchBox.Focus();
    }

    private bool MatchesSearch(object item)
    {
        if (item is not WindowCandidate window)
            return false;
        var query = SearchBox.Text.Trim();
        return query.Length == 0
            || window.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || window.ExecutableName.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = SearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (_windows is null)
            return;
        _windows.Refresh();
        UpdateResults();
    }

    private void UpdateResults()
    {
        int count = WindowList.Items.Count;
        CountText.Text = count == 1 ? UiText.T("1 programa") : $"{count} {UiText.T("programas")}";
        EmptyText.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        AddButton.IsEnabled = SelectedWindow is not null;
    }

    private void WindowList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        AddButton.IsEnabled = SelectedWindow is not null;

    private void WindowList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => AcceptSelection();

    private void Add_Click(object sender, RoutedEventArgs e) => AcceptSelection();

    private void AcceptSelection()
    {
        if (SelectedWindow is not null)
            DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && SelectedWindow is not null)
        {
            AcceptSelection();
            e.Handled = true;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1 && e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
}
