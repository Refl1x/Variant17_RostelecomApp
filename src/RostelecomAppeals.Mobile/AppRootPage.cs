namespace RostelecomAppeals.Mobile;

public sealed class AppRootPage : TabbedPage
{
    public AppRootPage()
    {
        Title = "Ростелеком";
        BarBackgroundColor = Colors.White;
        BarTextColor = Color.FromArgb("#5312C8");
        SelectedTabColor = Color.FromArgb("#7700FF");
        UnselectedTabColor = Color.FromArgb("#8E84A8");

        Children.Add(new NavigationPage(new AppealsPage())
        {
            Title = "Обращения",
            IconImageSource = "tab_home.svg",
            BarBackgroundColor = Color.FromArgb("#7700FF"),
            BarTextColor = Colors.White
        });

        Children.Add(new NavigationPage(new StatisticsPage())
        {
            Title = "Статистика",
            IconImageSource = "tab_stats.svg",
            BarBackgroundColor = Color.FromArgb("#7700FF"),
            BarTextColor = Colors.White
        });

        Children.Add(new NavigationPage(new SettingsPage())
        {
            Title = "Ещё",
            IconImageSource = "tab_settings.svg",
            BarBackgroundColor = Color.FromArgb("#7700FF"),
            BarTextColor = Colors.White
        });
    }
}
