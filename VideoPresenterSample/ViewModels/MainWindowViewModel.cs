using Avalonia.Media.Imaging;
using ReactiveUI;
using System.Reactive;

namespace VideoPresenterSample.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string m_Greeting = "Initial State 2";
    private double m_SpeedFactor = 1.0;
    public MainWindowViewModel()
    {
        UpdateGreetingCommand = ReactiveCommand.Create(
            () => Greeting = "This is the updated greeting from a command");
    }

    public string Greeting
    {
        get => m_Greeting;
        set => this.RaiseAndSetIfChanged(ref m_Greeting, value);
    }

    public ReactiveCommand<Unit, string> UpdateGreetingCommand { get; }

    public double SpeedFactor
    {
        get => m_SpeedFactor;
        set => this.RaiseAndSetIfChanged(ref m_SpeedFactor, value);
    }

}