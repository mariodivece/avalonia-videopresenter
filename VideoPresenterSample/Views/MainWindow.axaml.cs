using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using VideoPresenterSample.ViewModels;

namespace VideoPresenterSample.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //this.AttachDevTools();
            RendererDiagnostics.DebugOverlays |=
                Avalonia.Rendering.RendererDebugOverlays.Fps |
                Avalonia.Rendering.RendererDebugOverlays.RenderTimeGraph;
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            vm.Greeting = "Button was clicked!";
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
        }
    }
}