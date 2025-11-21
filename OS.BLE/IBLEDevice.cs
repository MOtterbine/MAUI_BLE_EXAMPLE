

namespace OS.BLE;

public interface IBLEDevice
{
    event BLEEvent BleEvent;
    bool IsEnabled { get; }
    bool IsConnected { get; }
    String Name { get;  }
    void StartScan();
    void StopScan();
    void Connect();
    void Disconnect();
}
