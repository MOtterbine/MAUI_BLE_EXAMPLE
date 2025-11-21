using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using Java.Util;
using System.Text;

namespace OS.BLE;

public class BLEGattCallback : BluetoothGattCallback
{
    public event GattEvent GattEvent;

    private AutoResetEvent gattOperationPending = new AutoResetEvent(true);

    /// <summary>
    /// Client Characteristic Configuration UUID
    /// </summary>
    private UUID CCC_DESCRIPTOR_UUID = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
    private async Task enableNotifications(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
    {
        GattProperty properties = characteristic.Properties;
        if ((properties & GattProperty.Notify) > 0)
        {
            gatt.SetCharacteristicNotification (characteristic, true);

            BluetoothGattDescriptor descriptor = characteristic.GetDescriptor(CCC_DESCRIPTOR_UUID);
            descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());

            // Don't enter with a pending operation - is freed in OnDescriptorWrite(...) 
            gattOperationPending.WaitOne();

            gatt.WriteDescriptor(descriptor);

        }
    }

    public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, [GeneratedEnum] GattStatus status)
    {
        base.OnDescriptorWrite(gatt, descriptor, status);
        // Signal that a write operation has completed
        gattOperationPending.Set();
    }

    /// <summary>
    /// Starts a task that sets up notifications for the characteristics passed in
    /// </summary>
    /// <param name="gatt"></param>
    /// <param name="_characteristicList"></param>
    public void SetupNotifications(BluetoothGatt gatt, IList<BluetoothGattCharacteristic> _characteristicList)
    {
        Task.Factory.StartNew(async () =>
        {
            // ensure threads are free to run at the start
            gattOperationPending.Set(); 

            foreach (var item in _characteristicList)
            {
                await enableNotifications(gatt, item);
            }
        });
    }

    public void ReadCharacteristics(BluetoothGatt gatt, IList<BluetoothGattCharacteristic> _characteristicList)
    {

        Task.Factory.StartNew(() =>
        {
            // start a threads pacing operation
            gattOperationPending.Set();

            foreach (var item in _characteristicList)
            {
                // Auto resets (blocks) after WaitOne is called - until gattOperationPending.Set() is called again
                gattOperationPending.WaitOne();

                gatt.ReadCharacteristic(item);
            }
        });
    }


    public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value, [GeneratedEnum] GattStatus status)
    {

        base.OnCharacteristicRead(gatt, characteristic, value, status);

        this.FireDataChangedEvent(characteristic, value);

        // Signal that a write operation has completed
        gattOperationPending.Set();

    }

    public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value)
    {

        base.OnCharacteristicChanged(gatt, characteristic, value);


        this.FireDataChangedEvent(characteristic, value);

        // Signal that a write operation has completed
        gattOperationPending.Set();

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

    public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, [GeneratedEnum] GattStatus status)
    {
        base.OnCharacteristicRead(gatt, characteristic, status);

     //   this.notifyCharacteristicUpdate(characteristic, value);


        // Signal that a write operation has completed
       // gattOperationPending.Set();

    }


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


    public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
    {
        base.OnCharacteristicChanged(gatt, characteristic);
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
