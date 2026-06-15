namespace RostelecomAppeals.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var navigationPage = new NavigationPage(new LoginPage())
        {
            BarBackgroundColor = Color.FromArgb("#7700FF"),
            BarTextColor = Colors.White
        };

        return new Window(navigationPage);
    }
}
