namespace MAUI_BLE_EXAMPLE;

public delegate void PermissionsResultReady(object sender, EventArgs e);

public partial class App : Application
{
    public bool HasPermissions { get; set; } = false;
    public event PermissionsResultReady PermissionsReady;
    public event EventHandler Sleep;
    public event EventHandler Resume;
    public event EventHandler AppClosing;
    public void NotifyAppClosing()
    {
        if (AppClosing == null) return;
        AppClosing(this, EventArgs.Empty);
    }
    public void FirePermissionsReadyEvent()
    {

        if (this.PermissionsReady != null)
        {
            this.PermissionsReady(null, EventArgs.Empty);
        }
    }

    public App()
    {

#if WINDOWS
        this.HasPermissions = true;
#endif

        InitializeComponent();

        // MainPage = new NavigationPage(new BLEPage());
       MainPage = new AppShell();
    }


    protected override Window CreateWindow(IActivationState activationState)
    {

        var window = base.CreateWindow(activationState);
        WindowInstance = window;

#if WINDOWS

        window.MinimumWidth = Constants.MIN_WINDOW_WIDTH_WINDOWS;
        window.MinimumHeight = Constants.MIN_WINDOW_HEIGHT_WINDOWS;

#endif

        return window;

    }
    public static Window WindowInstance { get; private set; }

    protected override void OnResume()
    {
        base.OnResume();
        if(Resume != null)
        {
            Resume(this, EventArgs.Empty);
        }
    }
    protected override void OnSleep()
    {
        if (Sleep != null)
        {
            Sleep(this, EventArgs.Empty);
        }
        base.OnSleep();
    }


}

