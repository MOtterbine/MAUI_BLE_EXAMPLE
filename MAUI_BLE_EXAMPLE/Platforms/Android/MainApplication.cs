using Android.App;
using Android.Runtime;

namespace MAUI_BLE_EXAMPLE;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
