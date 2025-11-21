using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Media;
using Java.Util;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using OS.BLE;

namespace MAUI_BLE_EXAMPLE;



public partial class MAUI_BLEDevice : OS.BLE.MAUI_BLEDevice, IMyBLEControl
{


    public MAUI_BLEDevice() : base(Constants.UUID_BLE_SERVICE_UUID)
    {

    }

    void IMyBLEControl.SetLightLevel(uint _percent)
    {
        WriteCharacteristicValue(UUID.FromString(Constants.UUID_BLE_CHARACTERISTIC_LEDPWM), (byte)_percent);
    }


    void IMyBLEControl.SetLight(bool _on)
    {
        byte sw = 0x00; // defaults to off (0x01)
        if (_on) sw = 0x01; // set to on
        WriteCharacteristicValue(UUID.FromString(Constants.UUID_BLE_CHARACTERISTIC_COMMAND), sw);
    }


    protected override void OnGattEvent(object sender, GattEventArgs e)
    {

        switch(e.EvenTypet)
        {
            case GattEventTypes.None:
                break;
            case GattEventTypes.Connected:
                _btGatt = e.data as BluetoothGatt;
                CancelScanTimout();
                FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Connecting));
                break;
            case GattEventTypes.Disconnected:
                CloseGattOperations();

                FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Disconnected));
                // try to reconnect... 
                StartScan();
                break;
            case GattEventTypes.ServicesDiscovered:
                IList<BluetoothGattService> gattServices = e.data as IList<BluetoothGattService>;

                if (gattServices == null) return;

                if (DiscoveredGattServices.Count > 0) // already been here, done this
                {
                    break;
                }

                // Clear out any saved services 
                DiscoveredGattServices.Clear();

                // Save the newly-discovered services
                DiscoveredGattServices.AddRange(gattServices);

                try
                {
                    RemoteModuleCharacteristics.Clear();

                    var targetService = DiscoveredGattServices.Where(s => s.Uuid.CompareTo(UUID.FromString(Constants.UUID_BLE_SERVICE_UUID)) == 0).FirstOrDefault();
                    if (targetService == null)
                    {
                        return;
                    }
                    foreach (var item in targetService.Characteristics)
                    {
                        RemoteModuleCharacteristics.Add(item);
                    }
                }
                catch { }

                // ******************* Initial Read of data FROM the device *********************
                ((BLEGattCallback)sender).ReadCharacteristics(_btGatt, RemoteModuleCharacteristics);

                // ******************* Setup for notifications for changes FROM the device *********************
                ((BLEGattCallback)sender).SetupNotifications(_btGatt, RemoteModuleCharacteristics);

                FireDeviceEvent(new BLEEventArgs(BLEEventTypes.ConnectedAsClient));


                break;
            case GattEventTypes.DataReady: // *********** Device-Initiated UI Updates **********
                var characteristicData = e.characteristic;// data as Tuple<string, object>;
                if (characteristicData == null) return;

                switch(characteristicData.Uuid.ToString().ToUpper())
                {
                    case Constants.UUID_BLE_CHARACTERISTIC_COMMAND: // button
                        using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.RemoteDataReady))
                        {
                            evt.data = ((byte[])e.data)[0];
                            evt.Description = "LEDValue";
                            FireDeviceEvent(evt);
                        }
                        break;
                    case Constants.UUID_BLE_CHARACTERISTIC_BUTTON: // button
                        using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.RemoteDataReady))
                        {
                            evt.data = ((byte[])e.data)[0];
                            evt.Description = "ButtonValue";
                            FireDeviceEvent(evt);
                        }
                        break;
                    case Constants.UUID_BLE_CHARACTERISTIC_TEMPERATURE: // temperature
                        using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.RemoteDataReady))
                        {
                            evt.data = BitConverter.ToDouble((byte[])e.data);
                            evt.Description = "Temperature";
                            FireDeviceEvent(evt);
                        }
                        break;
                    case Constants.UUID_BLE_CHARACTERISTIC_MESSAGE:
                        using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.RemoteDataReady))
                        {
                            evt.data = System.Text.Encoding.ASCII.GetString((byte[])e.data);
                            evt.Description = "DeviceMessage";
                            FireDeviceEvent(evt);
                        }
                        break;
                    case Constants.UUID_BLE_CHARACTERISTIC_LEDPWM:
                        using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.RemoteDataReady))
                        {
                            evt.data = ((byte[])e.data)[0];
                            evt.Description = "PWM";
                            FireDeviceEvent(evt);
                        }
                        break;
                }
                break;
        }

    }


}
