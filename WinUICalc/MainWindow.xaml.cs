using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinUICalc.ViewModels;

namespace WinUICalc;

/// <summary>
/// The application window. Hosts the calculator UI directly (no Frame/Page —
/// the calculator is a single-screen utility).
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Open at a sensible calculator-shaped size. Users can still resize.
        AppWindow.Resize(new SizeInt32(420, 600));
    }
}

