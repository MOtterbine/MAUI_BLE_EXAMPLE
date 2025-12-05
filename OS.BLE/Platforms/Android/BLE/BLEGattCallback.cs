using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using Java.Util;

namespace OS.BLE;

public class BLEGattCallback : BluetoothGattCallback
{
    public event GattEvent GattEvent;

    /// <summary>
    /// For characteristic read/write and descriptor read/write synchronization
    /// </summary>
    private static AutoResetEvent gattOperationPending = new AutoResetEvent(true);

    /// <summary>
    /// Client Characteristic Configuration UUID
    /// </summary>
    private UUID CCC_DESCRIPTOR_UUID = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
    /// <summary>
    /// Starts a task that sets up value-changed notifications for the characteristics passed in. Also does an initial read.
    /// </summary>
    /// <param name="gatt"></param>
    /// <param name="_characteristicList"></param>
    public void BindCharacteristics(BluetoothGatt gatt, IList<BluetoothGattCharacteristic> _characteristicList)
    {
        // Initial Read of Characteristics
        foreach (var item in _characteristicList)
        {
            readCharacteristic(gatt, item);
        }

        // Value-Changed Notifications
        foreach (var item in _characteristicList)
        {
            enableNotifications(gatt, item);
        }
    }

    private void readCharacteristic(BluetoothGatt gatt, BluetoothGattCharacteristic _characteristic)
    {
        // Read the value
        gatt.ReadCharacteristic(_characteristic);

        // wait until read is complete
        gattOperationPending.WaitOne();
    }

    private void enableNotifications(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
    {
        GattProperty properties = characteristic.Properties;
        if ((properties & GattProperty.Notify) > 0)
        {

            // Set the charactersistic notification flag
            gatt.SetCharacteristicNotification (characteristic, true);

            BluetoothGattDescriptor descriptor = characteristic.GetDescriptor(CCC_DESCRIPTOR_UUID);
            descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());

            // Write the descriptor to enable notifications.
            gatt.WriteDescriptor(descriptor);

            // wait until write is completed
            gattOperationPending.WaitOne();

        }
    }

    public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, [GeneratedEnum] GattStatus status)
    {
        base.OnDescriptorWrite(gatt, descriptor, status);
        // Signal the operation as completed
        gattOperationPending.Set();
    }

    public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value, [GeneratedEnum] GattStatus status)
    {
        base.OnCharacteristicRead(gatt, characteristic, value, status);
    }

    public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, [GeneratedEnum] GattStatus status)
    {
        base.OnCharacteristicRead(gatt, characteristic, status);

        this.FireDataChangedEvent(characteristic, characteristic.GetValue());

        // Signal that the operation has completed
        gattOperationPending.Set();

    }

    public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value)
    {
        base.OnCharacteristicChanged(gatt, characteristic, value);
    }

    public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
    {
        base.OnCharacteristicChanged(gatt, characteristic);

        this.FireDataChangedEvent(characteristic, characteristic.GetValue());

    }
    public override void OnConnectionStateChange(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status, [GeneratedEnum] ProfileState newState)
    {
        base.OnConnectionStateChange(gatt, status, newState);

        switch (newState)
        {
            case ProfileState.Connected:
                if (status == GattStatus.Success)
                {
                    if (gatt.Connect())
                    {
                        FireEvent(new GattEventArgs(GattEventTypes.Connected) { data = gatt });
                        gatt.DiscoverServices();
                    }
                }
                break;
            case ProfileState.Disconnected:
                FireEvent(new GattEventArgs(GattEventTypes.Disconnected));
                break;
        }
    }

    List<BluetoothGattService> discoverdServices = new List<BluetoothGattService>();
    public override void OnServicesDiscovered(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status)
    {
        base.OnServicesDiscovered(gatt, status);

        if (gatt == null) return;

        // FIre an event...
        using (GattEventArgs evt = new GattEventArgs(GattEventTypes.ServicesDiscovered))
        {
            evt.data = gatt.Services;
            this.FireEvent(evt);
        }

    }

    #region unused overrides



    public override void OnServiceChanged(BluetoothGatt gatt)
    {
        base.OnServiceChanged(gatt);
    }

    public override void OnReliableWriteCompleted(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status)
    {
        base.OnReliableWriteCompleted(gatt, status);
    }

    public override void OnPhyRead(BluetoothGatt? gatt, [GeneratedEnum] ScanSettingsPhy txPhy, [GeneratedEnum] ScanSettingsPhy rxPhy, [GeneratedEnum] GattStatus status)
    {
        base.OnPhyRead(gatt, txPhy, rxPhy, status);
    }

    public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, [GeneratedEnum] GattStatus status)
    {
        base.OnCharacteristicWrite(gatt, characteristic, status);
    }

    public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, [GeneratedEnum] GattStatus status, byte[] value)
    {
        base.OnDescriptorRead(gatt, descriptor, status, value);
    }

    public override void OnDescriptorRead(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, [GeneratedEnum] GattStatus status)
    {
        base.OnDescriptorRead(gatt, descriptor, status);
    }

    public override void OnPhyUpdate(BluetoothGatt? gatt, [GeneratedEnum] ScanSettingsPhy txPhy, [GeneratedEnum] ScanSettingsPhy rxPhy, [GeneratedEnum] GattStatus status)
    {
        base.OnPhyUpdate(gatt, txPhy, rxPhy, status);
    }

    public override void OnReadRemoteRssi(BluetoothGatt? gatt, int rssi, [GeneratedEnum] GattStatus status)
    {
        base.OnReadRemoteRssi(gatt, rssi, status);
    }




    public override void OnMtuChanged(BluetoothGatt? gatt, int mtu, [GeneratedEnum] GattStatus status)
    {
        base.OnMtuChanged(gatt, mtu, status);
    }



#endregion unused overrides


    private void FireDataChangedEvent(BluetoothGattCharacteristic _characteristic, object value)
    {
        if (this.GattEvent == null) return;

        using (GattEventArgs evt = new GattEventArgs(GattEventTypes.DataReady))
        {
            evt.characteristic = _characteristic;
            evt.data = value;
            this.FireEvent(evt);
        }
    }

    private void FireEvent(GattEventArgs evt)
    {
        if (this.GattEvent == null) return;
        this.GattEvent(this, evt);
    }


};
