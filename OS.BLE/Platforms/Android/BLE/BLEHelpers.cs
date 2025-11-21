using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using Microsoft.Extensions.Logging;
using System;

namespace OS.BLE;


public static class BLEHelpers
{

    public static ushort ConvertUuidToShortId(Guid uuid)
    {
        // Get the short Uuid
        var bytes = uuid.ToByteArray();
        var shortUuid = (ushort)(bytes[0] | (bytes[1] << 8));
        return shortUuid;
    }

    private static bool IsSigDefinedUuid(Guid uuid)
    {
        var bluetoothBaseUuid = new Guid("00000000-0000-1000-8000-00805F9B34FB");

        var bytes = uuid.ToByteArray();
        // Zero out the first and second bytes
        // Note how each byte gets flipped in a section - 1234 becomes 34 12
        // Example Guid: 35918bc9-1234-40ea-9779-889d79b753f0
        //                   ^^^^
        // bytes output = C9 8B 91 35 34 12 EA 40 97 79 88 9D 79 B7 53 F0
        //                ^^ ^^
        bytes[0] = 0;
        bytes[1] = 0;
        var baseUuid = new Guid(bytes);
        return baseUuid == bluetoothBaseUuid;
    }


    public static string GetServiceName(Android.Bluetooth.BluetoothGattService service)
    {
        var svcGuid = new Guid(service.Uuid.ToString());
        if (IsSigDefinedUuid(svcGuid))
        {
            GattNativeServiceUuid serviceName;
            if (System.Enum.TryParse(ConvertUuidToShortId(svcGuid).ToString(), out serviceName))
            {
                return serviceName.ToString();
            }
        }
        return "Custom Service: " + service.Uuid;
    }


    public static string GetCharacteristicName(BluetoothGattCharacteristic characteristic)
    {
        var charGuid = new Guid(characteristic.Uuid.ToString());
        if (IsSigDefinedUuid(charGuid))
        {
            GattNativeCharacteristicUuid characteristicName;
            if (System.Enum.TryParse(ConvertUuidToShortId(charGuid).ToString(),
                out characteristicName))
            {
                return characteristicName.ToString();
            }
        }

        if (!string.IsNullOrEmpty(characteristic.ToString()))
        {
            return characteristic.ToString();
        }

        else
        {
            return "Custom Characteristic: " + characteristic.Uuid;
        }
    }


}