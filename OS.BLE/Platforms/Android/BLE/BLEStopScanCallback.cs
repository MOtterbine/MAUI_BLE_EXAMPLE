using Android.Bluetooth.LE;
using Android.Runtime;

namespace OS.BLE;

public class BLEStopScanCallback : ScanCallback
{
    public override void OnBatchScanResults(IList<ScanResult>? results)
    {
        base.OnBatchScanResults(results);
    }
    public override void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
    {
        base.OnScanFailed(errorCode);
    }

    public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, ScanResult? result)
    {
        base.OnScanResult(callbackType, result);
    }

}
