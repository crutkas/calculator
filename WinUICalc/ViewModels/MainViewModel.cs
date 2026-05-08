using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinUICalc.Core;

namespace WinUICalc.ViewModels;

/// <summary>
/// View-model that adapts <see cref="CalculatorEngine"/> for x:Bind
/// consumption from <c>MainWindow.xaml</c>.
/// Each command simply delegates to the engine, then refreshes the
/// observable <see cref="Display"/> property so XAML re-renders.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly CalculatorEngine _engine = new();

    [ObservableProperty]
    public partial string Display { get; set; } = "0";

    [RelayCommand]
    private void Digit(string? digit)
    {
        if (string.IsNullOrEmpty(digit))
        {
            return;
        }
        _engine.InputDigit(digit[0]);
        Refresh();
    }

    [RelayCommand]
    private void Decimal()
    {
        _engine.InputDecimal();
        Refresh();
    }

    [RelayCommand]
    private void Operator(string? op)
    {
        if (string.IsNullOrEmpty(op))
        {
            return;
        }

        Operator? mapped = op switch
        {
            "+" => WinUICalc.Core.Operator.Add,
            "-" or "−" => WinUICalc.Core.Operator.Subtract,
            "*" or "×" => WinUICalc.Core.Operator.Multiply,
            "/" or "÷" => WinUICalc.Core.Operator.Divide,
            _ => null,
        };
        if (mapped is null)
        {
            return;
        }

        _engine.SetOperator(mapped.Value);
        Refresh();
    }

    [RelayCommand]
    private void Equals()
    {
        _engine.Equals();
        Refresh();
    }

    [RelayCommand]
    private void Clear()
    {
        _engine.Clear();
        Refresh();
    }

    [RelayCommand]
    private void Backspace()
    {
        _engine.Backspace();
        Refresh();
    }

    private void Refresh() => Display = _engine.Display;
}
