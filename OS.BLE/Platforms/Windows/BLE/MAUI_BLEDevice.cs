
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Application = Microsoft.Maui.Controls.Application;

namespace OS.BLE;


public abstract partial class MAUI_BLEDevice : IBLEDevice
{
    public string Name => (_mainDevice == null) ? Constants.STRING_NONE : _mainDevice.Name;

    protected GattDeviceService _mainService = null;
    protected BluetoothLEDevice _mainDevice = null;

    #region BLE Scan Timeout

    protected System.Threading.Timer _CommTimer = null;
    protected void OnCommTimeout(object sender)
    {
        CancelScanTimout();
        // Just, restart the scan - forever
        StartScan();
    }
    /// <summary>
    /// Starts and enables RX timeout timer
    /// </summary>
    private void RestartScanTimer()
    {
        // Reset RX timeout timer
        this._CommTimer.Change(Constants.DEFAULT_COMM_NO_RESPONSE_TIMEOUT, Constants.DEFAULT_COMM_NO_RESPONSE_TIMEOUT);
    }

    /// <summary>
    /// Stops RX timeout timer
    /// </summary>
    private void CancelScanTimout()
    {
        // Reset RX timeout timer to infinite (stop)
        this._CommTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    #endregion  BLE Scan Timeout

    private BluetoothLEAdvertisementWatcher _bluetoothDeviceWatcher = new BluetoothLEAdvertisementWatcher();
    /// <summary>
    /// Starts a device watcher that looks for all nearby Bluetooth devices (paired or unpaired).
    /// Attaches event handlers to populate the device collection.
    /// </summary>
    private void StartBleDeviceWatcher(Guid _serviceGuid)
    {
        
        _bluetoothDeviceWatcher.ScanningMode = BluetoothLEScanningMode.Active;
        _bluetoothDeviceWatcher.Received += OnBTWatcherReceived;
        // Filter for the relevant devices
        //  _bluetoothDeviceWatcher.AdvertisementFilter.Advertisement.LocalName = Constants.UUID_BLE_HARDWARE_DEVICE_NAME;
        _bluetoothDeviceWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(_serviceGuid);

        _bluetoothDeviceWatcher.Start();

    }

    private async void OnBTWatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        if (args.IsScannable)
        {

            _mainDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);//FromIdAsync(_devId);

            if (_mainDevice == null)
            {
                FireErrorEvent("Couldn't acquire device");
                return;
            }
            if (await TestDeviceForService(_mainDevice, this._mainServiceUUID))
            {
                StopScan();
                FireDeviceEvent(new BLEEventArgs(BLEEventTypes.ScanResultsReceived));
            }

        }
    }

    /// <summary>
    /// Stops watching for all nearby Bluetooth devices.
    /// </summary>
    private void StopBleDeviceWatcher()
    {
        if (_bluetoothDeviceWatcher != null)
        {
            _bluetoothDeviceWatcher.Received -= OnBTWatcherReceived;
            _bluetoothDeviceWatcher?.Stop();

        }
    }

    /// <summary>
    /// To help ensure async unbind operations are completed. Used mainly for app closing.
    /// </summary>
    AutoResetEvent unbindingResetEvent = new AutoResetEvent(true);

    private async Task<bool> TestDeviceForService(BluetoothLEDevice _device, Guid _svcId)
    {
        if (_device != null)
        {
            return await GetAndConnectService(_device, _svcId);
        }
        return false;
    }

    private async Task<bool> GetAndConnectService(BluetoothLEDevice _device, Guid _svcId)
    {
        _mainService = await GetService(_device, _svcId);
        if (_mainService == null) return false;
        _mainDevice = _device;
        return true;
    }

    private async Task<GattDeviceService> GetService(BluetoothLEDevice _device, Guid _svcId)
    {
        GattDeviceServicesResult result = null;
        try
        {
            result = await _device.GetGattServicesAsync(BluetoothCacheMode.Cached);
            if (result == null || result.Services == null || result.Services.Count < 1)
            {
                return null;
            }
            return result.Services.Where(s => s.Uuid.CompareTo(_svcId) == 0).FirstOrDefault();
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception e)
        {
        }
        return null;
    }

    object syncLock = new object();
    private async Task<bool> BindService(GattDeviceService _bleService)
    {
        try
        {
            // Ensure we have access to the device.
            DeviceAccessStatus accessStatus = await _bleService.RequestAccessAsync();
            if (accessStatus != DeviceAccessStatus.Allowed)
            {
                // Not granted access
                FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Disconnected));
                FireErrorEvent($"{accessStatus} ");
                return false;
            }
        }
        catch (TaskCanceledException)
        {
            FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Disconnected));
            FireErrorEvent($"Task cancelled");
            return false;
        }
        catch (Exception e)
        {
            FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Disconnected));
            FireErrorEvent($"{e.Message} ");
            return false;
        }
        GattCharacteristicsResult result = null;
        try
        {
            result = await _bleService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (result.Status != GattCommunicationStatus.Success)
            {
                FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Disconnected));
                FireErrorEvent($"{result.Status}");
                return false;
            }

        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (Exception e)
        {
            return false;
        }

        if (result.Characteristics == null) return false;

        if (_mainDevice != null)
        {
            _mainDevice.ConnectionStatusChanged += OnBLEDeviceConnectionStatusChanged;
        }
            try
            {
                lock (bindLock)
                {
                    // keep a reference to the characteristics
                    foreach (var item in result.Characteristics)
                    {
                        RemoteModuleCharacteristics.Add(item);
                    }

                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        // connect the events
                        foreach (var item in RemoteModuleCharacteristics)
                        {
                            Subscribe(item);
                        }
                    });
                }
            }
            catch (Exception e) {
            FireErrorEvent($"Error binding: {e.Message}");

        }

        return true;
    }

    private void OnBLEDeviceConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (_mainDevice == null)
        {
            return;
        }

        switch (_mainDevice.ConnectionStatus)
        {
            case BluetoothConnectionStatus.Connected:
                break;
            case BluetoothConnectionStatus.Disconnected:
                CloseGattOperations();

                FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Disconnected));
                // try to reconnect...
                StartScan();
                break;
        }
    }

    private void UnbindService(GattDeviceService _bleService)
    {
        //Application.Current.Dispatcher.Dispatch(async() =>
        Task.Factory.StartNew(async () =>
        {
            try
            {
                // disconnect the events
                foreach (var item in RemoteModuleCharacteristics)
                {
                    await ClearBluetoothLEDeviceAsync(item);
                }
                if (_mainDevice != null)
                {
                    _mainDevice.ConnectionStatusChanged -= OnBLEDeviceConnectionStatusChanged;
                }
            }
            catch { }
            finally
            {
                RemoteModuleCharacteristics.Clear();
                _mainService?.Dispose();
                _mainService = null;
                unbindingResetEvent.Set();
            }
        });

    }

    private async Task ClearBluetoothLEDeviceAsync(GattCharacteristic characteristic)
    {
        // Capture the characteristic we want to unregister, in case the user changes it during the await.
        if (characteristic != null)
        {
            try
            {
                // Clear the CCCD from the remote device so we stop receiving notifications
                GattCommunicationStatus result = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                if (result != GattCommunicationStatus.Success)
                {
                    // Even if we are unable to unsubscribe, continue with the rest of the cleanup.
                }
                characteristic.ValueChanged -= Characteristic_ValueChanged;
            }
            catch { }// usually caused by a device disconnect while the app was a client
        }
    }

    private async void Subscribe(GattCharacteristic _characteristic)
    {

        // initialize status
        var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.None;
        if (_characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
        {
            // Subscribe with "indicate"
            cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
        }
        else
        if (_characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
        {
            // Subscribe with "notify"
            cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
        }
        else
        {
            // Unreachable 
        }

        try
        {
            GattWriteResult result = await _characteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(cccdValue);

            if (result.Status == GattCommunicationStatus.Success)
            {
                if (cccdValue != GattClientCharacteristicConfigurationDescriptorValue.None)
                {
                    AddValueChangedHandler(_characteristic);
                }
                else
                {
                    RemoveValueChangedHandler(_characteristic);
                }
            }
            else
            {
                // This can happen when a device doesn't support what it reports.
            }
        }
        catch (ObjectDisposedException)
        {
            // Service is no longer available.
            //  rootPage.NotifyUser($"{operation} failed: Service is no longer available.", NotifyType.ErrorMessage);
        }
        catch (Exception)
        {
            // unknown;
        }
    }

    private void AddValueChangedHandler(GattCharacteristic _characteristic)
    {
        if (_characteristic != null)
        {
            _characteristic.ValueChanged += Characteristic_ValueChanged;
        }
    }

    private void RemoveValueChangedHandler(GattCharacteristic _characteristic)
    {
        if (_characteristic != null)
        {
            _characteristic.ValueChanged -= Characteristic_ValueChanged;
        }
    }

    private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        HandleBLEDataUpdate(sender.Uuid.ToString().ToUpper(), args.CharacteristicValue.ToArray());
    }

    protected virtual void HandleBLEDataUpdate(String _characteristicId, byte[] _buffer)
    {
        // intended to be handled in overridden method in derived class
    }

    protected void WriteCharacteristicValue(Guid charUuid, byte [] val)
    {

        // find the charactersitic by UUID
        var ch = RemoteModuleCharacteristics.Where(c => c.Uuid.CompareTo(charUuid) == 0).FirstOrDefault();

        // can we even do this?
        if (ch == null) return;

        try
        {
            // Set the value on the device
            ch.WriteValueAsync(val.AsBuffer());
        }
        catch
        {

        }
    }
    protected void WriteCharacteristicValue(Guid charUuid, byte val)
    {

        // find the charactersitic by UUID
        var ch = RemoteModuleCharacteristics.Where(c => c.Uuid.CompareTo(charUuid) == 0).FirstOrDefault();

        // can we even do this?
        if (ch == null) return;

        try
        {
            // Set the value on the device
            ch.WriteValueAsync((new byte[] { val }).AsBuffer());
        }
        catch
        {

        }
    }

    // ELM327 uart rx buffer is 512 bytes
    public const int BUFFER_SIZE = 2048;
    public event BLEEvent DeviceEvent;

    private byte[] RespBuffer = new byte[BUFFER_SIZE];
    private byte[] TmpBuffer = new byte[BUFFER_SIZE];
    private CancellationTokenSource tokenSource = null;// new CancellationTokenSource();

    public bool IsEnabled => this._mainDevice != null && this._mainDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;

    public List<GattCharacteristic> RemoteModuleCharacteristics { get; } = new List<GattCharacteristic>();

    private Guid _mainServiceUUID = Guid.Empty;

    public MAUI_BLEDevice(String _serviceUuid)
    {
        try
        {
            this._mainServiceUUID = Guid.Parse(_serviceUuid);
        }
        catch
        {
            throw new ArgumentException($"Unable to parse service uuid. value: {_serviceUuid}");
        }
        // Create a Communication Timeout (so we don't get stuck)
        _CommTimer = new System.Threading.Timer(this.OnCommTimeout, null, Timeout.Infinite, Timeout.Infinite);

    }

    private void BTReceiver_BluetoothDisabled(object? sender, EventArgs e)
    {
        CloseGattOperations();
        FireErrorEvent(Constants.STRING_BLUETOOTH_DISABLED);
    }

    protected void BTReceiver_BluetoothEnabled(object? sender, EventArgs e)
    {
        StartScan();
    }



    public void StartScan()//String _serviceUuid)
    {

        StartBleDeviceWatcher(this._mainServiceUUID);
    }

    public void StopScan()
    {
        CancelScanTimout();
        StopBleDeviceWatcher();
    }

    protected void CloseGattOperations()
    {
        if (this._bluetoothDeviceWatcher != null)
        {
            StopBleDeviceWatcher();
        }

        unbindingResetEvent.Reset(); // blocking
        try
        {
            UnbindService(_mainService);
        }
        catch { }


        this._mainDevice?.Dispose();
        this._mainDevice = null;
        unbindingResetEvent.WaitOne(); // wait until async ops are finished
    }
    /// <summary>
    /// Avoid collection enumeration exceptions
    /// </summary>
    object bindLock = new object();
    public void Connect()
    {
        if (this._mainService == null)
        {
            FireErrorEvent(" Main service is null.");
            return;
        }
        FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Connecting));
        try
        {
            lock (bindLock)
            {
                // ******** INITIAL READ OF CHARACTERISTIC VALUES **************
                Task.Factory.StartNew(async () =>
                //Application.Current.Dispatcher.Dispatch(async () =>
                {
                    if (await BindService(_mainService))
                    {
                        FireDeviceEvent(new BLEEventArgs(BLEEventTypes.ConnectedAsClient));

                        foreach (var item in this.RemoteModuleCharacteristics)
                        {
                            GattReadResult result = await item.ReadValueAsync(BluetoothCacheMode.Uncached);
                            if (result.Status == GattCommunicationStatus.Success)
                            {
                                HandleBLEDataUpdate(item.Uuid.ToString().ToUpper(), result.Value.ToArray());
                            }
                        }
                    }

                });
            }
        }
        catch (Exception e) {
            FireErrorEvent($"Error connecting: {e.Message}");
        }

    }

    public bool IsConnected => this._mainDevice == null ? false : this._mainDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;

    protected void FireDeviceEvent(BLEEventArgs e)
    {
        if (this.BleEvent != null)
        {
            BleEvent(this, e);
        }
    }

    public event BLEEvent BleEvent;

    protected void FireErrorEvent(string message)
    {
        using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.Error))
        {
            evt.Description = message;
            this.FireDeviceEvent(evt);
        }
    }

    public override string ToString()
    {
        return (this._mainDevice == null) ? Constants.STRING_NONE : this._mainDevice.Name;
    }


    public void Disconnect()
    {
        CloseGattOperations();
    }

}
