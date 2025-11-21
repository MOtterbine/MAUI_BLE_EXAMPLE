
using OS.BLE;
namespace MAUI_BLE_EXAMPLE;

public interface IMyBLEControl : IBLEDevice
{
    void SetLightLevel(uint _percent);
    void SetLight(bool _on);
}
