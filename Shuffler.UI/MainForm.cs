using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Shuffler.UI.Components;

namespace Shuffler.UI;

public class MainForm : Form
{
    private IServiceProvider? _serviceProvider;

    public void Init(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
        InitializeBlazor();
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1920, 1080);
        Text = "Shuffler";
        WindowState = FormWindowState.Normal;

        // Set the form icon
        var iconPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "icons", "app.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);
    }

    private void InitializeBlazor()
    {
        var blazorWebView = new BlazorWebView()
        {
            Dock = DockStyle.Fill,
            HostPage = "wwwroot/index.html",
            Services = _serviceProvider!
        };

        blazorWebView.RootComponents.Add<Routes>("#app");
        Controls.Add(blazorWebView);
    }
}