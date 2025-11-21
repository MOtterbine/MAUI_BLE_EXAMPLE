
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Application = Microsoft.Maui.Controls.Application;
using OS.BLE;

namespace MAUI_BLE_EXAMPLE;


public partial class MAUI_BLEDevice : OS.BLE.MAUI_BLEDevice, IMyBLEControl
{

    public MAUI_BLEDevice() : base(Constants.UUID_BLE_SERVICE_UUID)
    {

    }


    public void SetLightLevel(uint _percent)
    {
        WriteCharacteristicValue(Guid.Parse(Constants.UUID_BLE_CHARACTERISTIC_LEDPWM), (byte)_percent);
    }

    protected override void HandleBLEDataUpdate(String _characteristicId, byte[] _buffer)
    {
        switch (_characteristicId)
        {
            case Constants.UUID_BLE_CHARACTERISTIC_COMMAND: // button
                using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.RemoteDataReady))
                {
                    evt.data = _buffer[0];
                    evt.Description = "LEDValue";
                    FireDeviceEvent(evt);
                }
                break;
            case Constants.UUID_BLE_CHARACTERISTIC_LEDPWM:
                using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.RemoteDataReady))
                {
                    evt.data = _buffer[0];
                    evt.Description = "PWM";
                    FireDeviceEvent(evt);
                }
                break;
            case Constants.UUID_BLE_CHARACTERISTIC_BUTTON: // button
                using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.RemoteDataReady))
                {
                    evt.data = _buffer[0];
                    evt.Description = "ButtonValue";
                    FireDeviceEvent(evt);
                }
                break;
            case Constants.UUID_BLE_CHARACTERISTIC_TEMPERATURE: // temperature
                using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.RemoteDataReady))
                {
                    evt.data = BitConverter.ToDouble(_buffer);
                    evt.Description = "Temperature";
                    FireDeviceEvent(evt);
                }
                break;
            case Constants.UUID_BLE_CHARACTERISTIC_MESSAGE:
                using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.RemoteDataReady))
                {
                    evt.data = Encoding.ASCII.GetString(_buffer);
                    //var s = new string(args.CharacteristicValue.ToArray());
                    //evt.data = .ToString();
                    evt.Description = "DeviceMessage";
                    FireDeviceEvent(evt);
                }
                break;
        }
    }


    void IMyBLEControl.SetLight(bool _on)
    {
        byte sw = 0x00; // defaults to off (0x01)
        if (_on) sw = 0x01; // set to on
        WriteCharacteristicValue(Guid.Parse(Constants.UUID_BLE_CHARACTERISTIC_COMMAND), sw);
    }




}
