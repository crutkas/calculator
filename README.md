# WinUICalc

A simple Fluent-styled desktop calculator built with **WinUI 3 / Windows App SDK**.
The calculator engine is a pure C# state machine in its own library so it can be
unit-tested without spinning up a UI, and the WinUI app is layered on top via
MVVM (CommunityToolkit.Mvvm).

![CI](https://github.com/crutkas/calculator/actions/workflows/ci.yml/badge.svg)

## Features

- Standard 4-function calculator (add, subtract, multiply, divide) with chaining
- Decimal input, backspace, clear
- Divide-by-zero / overflow → `Error` state, recoverable with `C`
- Full UI Automation support (every interactive control has an `AutomationId`
  and an accessible `Name`)
- Mica backdrop, custom `TitleBar`, accent-styled `=` button

## Project layout

```
WinUICalc.sln
├── WinUICalc.Core/          Pure C# class library (net9.0)
│                            └── CalculatorEngine state machine. UI-free, fully unit-testable.
├── WinUICalc/               WinUI 3 app (net10.0-windows10.0.26100.0, x86 / x64 / ARM64)
│                            ├── MainWindow.xaml       Calculator surface
│                            └── ViewModels/MainViewModel.cs   x:Bind target, wraps the engine
├── WinUICalc.Tests/         xUnit unit tests (net9.0) for CalculatorEngine
├── ui-tests/
│   └── ui-tests.ps1         End-to-end UIA-driven tests (run against a live app process)
├── global.json              Pins .NET SDK 10.0.203 (rollForward: latestFeature)
└── .github/workflows/ci.yml Build + unit tests + UI tests on every push / PR
```

## Prerequisites

- **Windows 11** (the app targets `10.0.26100.0`; Windows 10 1809+ at runtime)
- **.NET SDK 10** matching `global.json` (`10.0.203` or newer 10.0.x)
- **Developer Mode** enabled (Settings → Privacy & security → For developers)
  — required for the debug package identity used by `dotnet run` / `winapp run`
- **Visual Studio 2022** is *not* required. The solution builds with
  `dotnet` only. ⚠ VS 2022's bundled MSBuild 17.14 cannot host .NET SDK 10
  (`MSB4236 / NETSDK1045`) — always use `dotnet build` / `dotnet test`,
  never `msbuild`.

## Local commands

From the solution root:

```powershell
# Build everything (Release, x64)
dotnet restore WinUICalc.sln -p:Platform=x64
dotnet build   WinUICalc.sln -c Release -p:Platform=x64 --no-restore

# Run the unit tests (29 tests covering CalculatorEngine)
dotnet test WinUICalc.Tests\WinUICalc.Tests.csproj -c Release --no-build

# Run the app (registers a debug package identity, launches via AUMID)
dotnet run --project WinUICalc\WinUICalc.csproj -c Release -p:Platform=x64

# Run the UI tests against a launched instance
#   1. start the app and grab its PID, then:
pwsh -File ui-tests\ui-tests.ps1 -AppPid <pid>
```

The CI workflow does the same thing on `windows-latest` for every push and PR.

## CI

`.github/workflows/ci.yml` runs on `windows-latest` and:

1. Installs the .NET SDK pinned in `global.json`.
2. Restores and builds the solution at `Release|x64`.
3. Runs unit tests (`xUnit`, results published as `unit.trx`).
4. Publishes the WinUI app to a loose layout, registers it with `winapp run`,
   and runs `ui-tests\ui-tests.ps1` against the live process.
5. Uploads the `.trx` results, the UI test JSON, and the final screenshot
   as workflow artifacts.

## License

Private project.
