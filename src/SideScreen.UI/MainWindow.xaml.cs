using System.Windows;
using SideScreen.UI.ViewModels;

namespace SideScreen.UI;

/// <summary>
/// Fase 2: shell MVVM com navegação. DataContext = MainViewModel.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
