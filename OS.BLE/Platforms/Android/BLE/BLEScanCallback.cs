using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;

namespace OS.BLE;


public class BLEScanCallback : ScanCallback
{

    public List<BluetoothDevice> Devices { get; } = new List<BluetoothDevice>();
    public string Address { get; private set; }
    public Android.OS.ParcelUuid[] Uuids { get; private set; }
    public string DeviceName { get; private set; }

    public override void OnBatchScanResults(IList<ScanResult>? results)
    {
        base.OnBatchScanResults(results);
    }
    public override void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
    {
        base.OnScanFailed(errorCode);
    }

    public event EventHandler ScanResultReceived;

    public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, ScanResult? result)
    {
        base.OnScanResult(callbackType, result);

        Address = result.Device.Address;
        Uuids = result.Device.GetUuids();

        DeviceName = result.Device.Name;

        Console.WriteLine($"{DeviceName} - {Address}");
        if (!Devices.Contains(result.Device))
        {
            Devices.Add(result.Device);
        }


        if (ScanResultReceived != null) ScanResultReceived(this, EventArgs.Empty);

    }

};