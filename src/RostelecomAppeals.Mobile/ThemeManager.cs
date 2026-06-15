namespace RostelecomAppeals.Mobile;

public static class ThemeManager
{
    public static bool IsDarkBackground { get; private set; }

    public static event Action? ThemeChanged;

    public static Color BackgroundColor => IsDarkBackground ? Color.FromArgb("#0F0F14") : Color.FromArgb("#F5F3FB");

    public static void ToggleBackgroundTheme()
    {
        IsDarkBackground = !IsDarkBackground;
        ApplyToApplication();
        ThemeChanged?.Invoke();
    }

    public static void ApplyToApplication()
    {
        var root = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (root == null) return;
        ApplyToPageTree(root);
    }

    private static void ApplyToPageTree(Page page)
    {
        page.BackgroundColor = BackgroundColor;

        if (page is TabbedPage tabs)
        {
            foreach (var child in tabs.Children)
            {
                ApplyToPageTree(child);
            }
        }
        else if (page is NavigationPage nav)
        {
            nav.BackgroundColor = BackgroundColor;
            if (nav.CurrentPage != null)
            {
                ApplyToPageTree(nav.CurrentPage);
            }
        }
        else if (page is FlyoutPage flyout)
        {
            if (flyout.Detail != null) ApplyToPageTree(flyout.Detail);
            if (flyout.Flyout != null) ApplyToPageTree(flyout.Flyout);
        }
    }
}
