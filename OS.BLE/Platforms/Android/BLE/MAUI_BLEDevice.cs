using Android.Bluetooth;
using Android.Bluetooth.LE;
using Java.Util;
using Microsoft.Maui.Controls;
using System.Collections.Generic;

namespace OS.BLE;



public  partial class MAUI_BLEDevice : IBLEDevice
{

    #region BLE Scan Timeout

    private System.Threading.Timer _CommTimer = null;
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
    protected void CancelScanTimout()
    {
        // Reset RX timeout timer to infinite (stop)
        this._CommTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    #endregion  BLE Scan Timeout

    protected void WriteCharacteristicValue(UUID charUuid, byte val)
    {
        // find the charactersitic by UUID
        var ch = RemoteModuleCharacteristics.Where(c=>c.Uuid.CompareTo(charUuid) == 0).FirstOrDefault();

        // can we even do this?
        if (ch == null) return;

        try
        {
            // Set the value on the device
            ch.SetValue(new byte[] { val });
            _btGatt.WriteCharacteristic(ch);
        }
        catch
        {
        }
    }


    // ELM327 uart rx buffer is 512 bytes
    public const int BUFFER_SIZE = 2048;
    public event BLEEvent DeviceEvent;

    private BluetoothDevice bluetoothDevice = null;
    private BluetoothSocket bluetoothSocket = null;
    private BluetoothAdapter bluetoothAdapter = null;
    private byte[] RespBuffer = new byte[BUFFER_SIZE];
    private byte[] TmpBuffer = new byte[BUFFER_SIZE];
    private CancellationTokenSource tokenSource = null;// new CancellationTokenSource();

    public bool IsEnabled => this.bluetoothAdapter != null && this.bluetoothAdapter.IsEnabled;

    public BLEScanCallback _BLEScanCallback { get; private set; } = new BLEScanCallback();

    public List<BluetoothGattCharacteristic> RemoteModuleCharacteristics { get; } = new List<BluetoothGattCharacteristic>();
    protected List<BluetoothGattService> DiscoveredGattServices = new List<BluetoothGattService>();
    public BLEGattCallback gattCallback { get; private set; } = new BLEGattCallback();

    public BluetoothGatt _btGatt;

    BluetoothLeScanner scanner;
    private Guid _mainServiceUUID = Guid.Empty;

    public MAUI_BLEDevice(String _serviceUuid)
    {
        try
        {
            this._mainServiceUUID = Guid.Parse(_serviceUuid);
        }catch
        {
            throw new ArgumentException($"Unable to parse service uuid. value: {_serviceUuid}");
        }
        // Create a Communication Timeout (so we don't get stuck)
        _CommTimer = new System.Threading.Timer(this.OnCommTimeout, null, Timeout.Infinite, Timeout.Infinite);

        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        //(activity as MainActivity).BTReceiver.BluetoothEnabled += BTReceiver_BluetoothEnabled;
        //(activity as MainActivity).BTReceiver.BluetoothDisabled += BTReceiver_BluetoothDisabled;

    }


    protected virtual void OnGattEvent(object sender, GattEventArgs e)
    {
        // expected to be handled in overriden method in derived class 
    }

    void IBLEDevice.Disconnect() => CloseGattOperations();

    protected void CloseGattOperations()
    {
        
        _btGatt?.Close();
        _btGatt?.Dispose();
        _btGatt = null;
        RemoteModuleCharacteristics.Clear();
    }


    public void Connect()
    {

        if (this.bluetoothDevice == null)
        {
            return;
        }

        var btMgr = GetBluetoothManager();
        var gattConnectionState = btMgr.GetConnectionState(this.bluetoothDevice, ProfileType.Gatt);

        if (_btGatt != null)
        {
            switch(gattConnectionState)
            {
                case ProfileState.Connected:
                    FireDeviceEvent(new BLEEventArgs(BLEEventTypes.ConnectedAsClient));
                    return;
                case ProfileState.Connecting:
                    Application.Current.Dispatcher.Dispatch(() => {

                        FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Connecting));

                        // Get Gatt Services
                        gattCallback.GattEvent -= OnGattEvent;
                        gattCallback.GattEvent += OnGattEvent;

                        // device is set via callback  connectionchanged override
                        this.bluetoothDevice.ConnectGatt(Android.App.Application.Context, false, gattCallback);

                    });
                    return;
                case ProfileState.Disconnected:
                    FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Disconnected));
                    return;
                case ProfileState.Disconnecting:
                    FireDeviceEvent(new BLEEventArgs(BLEEventTypes.RemoteDisconnect));
                    return;
            }

        }

        Application.Current.Dispatcher.Dispatch(() => {

            FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Connecting));

            // Get Gatt Services
            gattCallback.GattEvent -= OnGattEvent;
            gattCallback.GattEvent += OnGattEvent;

            // device is set via callback  connectionchanged override
            this.bluetoothDevice.ConnectGatt(Android.App.Application.Context, false, gattCallback);

        });
        return;
          
    }

    void OnScanResults(object sender, EventArgs e)
    {
        CancelScanTimout();
        this._BLEScanCallback.ScanResultReceived -= OnScanResults;

        this.bluetoothDevice = _BLEScanCallback.CurrentDevice;

        scanner.StopScan(_BLEScanCallback);

        if (BleEvent != null) BleEvent(this, new BLEEventArgs(BLEEventTypes.ScanResultsReceived));
    }

    private BluetoothManager GetBluetoothManager()
    {
        BluetoothManager bManager = Platform.CurrentActivity.GetSystemService(Android.Content.Context.BluetoothService) as BluetoothManager;

        if (bManager == null || bManager.Adapter == null)
        {
            FireErrorEvent($"No BT Service Available");
            return null;
        }
        return bManager;
    }

    protected void BTReceiver_BluetoothEnabled(object? sender, EventArgs e)
    {
        StartScan();
    }

    protected void BTReceiver_BluetoothDisabled(object? sender, EventArgs e)
    {
        CloseGattOperations();
        FireErrorEvent(Constants.STRING_BLUETOOTH_DISABLED);
    }

    public void StopScan()
    {
        CancelScanTimout();
        _btGatt?.Disconnect();
    }


    public void StartScan()
    {
        BluetoothManager bManager = GetBluetoothManager();
        if (bManager == null) return;
        if(!bManager.Adapter.IsEnabled)
        {
            // Bluetooth is disabled...
            FireErrorEvent(Constants.STRING_BLUETOOTH_DISABLED); return;
        }
        StartBluetoothLE(bManager);
    }

    private void StartBluetoothLE(BluetoothManager _btManagerr)
    {

        try
        {
            if (_btGatt != null && bluetoothDevice != null)
            {
                if(_btGatt.ConnectedDevices!=null &&_btGatt.ConnectedDevices.Count > 0)
                {
                    foreach (var item in _btGatt.ConnectedDevices)
                    {
                        if(string.Compare(item.Address, this.bluetoothDevice.Address) == 0)
                        {
                            // we're already connected
                            FireDeviceEvent(new BLEEventArgs(BLEEventTypes.ConnectedAsClient));
                            return;
                        }
                    }
                }
            }

            // Start a timer to see if this hangs
        //    RestartScanTimer();
            FireDeviceEvent(new BLEEventArgs(BLEEventTypes.Scanning));

            DiscoveredGattServices?.Clear();

            this.bluetoothAdapter = _btManagerr.Adapter;
            scanner = this.bluetoothAdapter.BluetoothLeScanner;

            if (scanner == null) return;

            List<ScanFilter> filters = new List<ScanFilter>();
            ScanFilter.Builder filterBuilder = new ScanFilter.Builder();
            //filterBuilder.SetDeviceName(Constants.UUID_BLE_HARDWARE_DEVICE_NAME);
            //filterBuilder.SetDeviceAddress(Constants.MAC_ADDRESS_SUPER_MINI_0); .. specifies a specific board
            filterBuilder.SetServiceUuid(new Android.OS.ParcelUuid(UUID.FromString(this._mainServiceUUID.ToString())));
            filters.Add(filterBuilder.Build());

            ScanSettings.Builder settingsBuilder = new ScanSettings.Builder();
            settingsBuilder.SetScanMode(Android.Bluetooth.LE.ScanMode.LowLatency);
            settingsBuilder.SetCallbackType(ScanCallbackType.AllMatches);
            settingsBuilder.SetMatchMode(BluetoothScanMatchMode.Aggressive);
           // settingsBuilder.SetPhy(Android.Bluetooth.BluetoothPhy.
            //var py = Android.Bluetooth.BluetoothPhy.;
            //py.
            //settingsBuilder.SetPhy(new Android.Bluetooth.BluetoothPhy() { })

            _BLEScanCallback.ScanResultReceived -= OnScanResults;
            _BLEScanCallback.ScanResultReceived += OnScanResults;

            scanner.StartScan(filters, settingsBuilder.Build(), _BLEScanCallback);

        }
        catch (System.Exception e)
        {
            FireErrorEvent($"Unable to Start Scan. {e}");
        }
    }

    public string Name => _btGatt == null ? Constants.STRING_NONE : _btGatt.Device.Name;

    public bool IsConnected => this.bluetoothSocket == null ? false : this.bluetoothSocket.IsConnected;

    public string Description => $"Bluetooth device: {this.Name}";

    protected void FireDeviceEvent(BLEEventArgs e)
    {
        if (this.BleEvent != null)
        {
            BleEvent(this, e);
        }
    }

    public event BLEEvent BleEvent;

    private void FireErrorEvent(string message)
    {
        using (BLEEventArgs evt = new BLEEventArgs(BLEEventTypes.Error))
        {
            evt.Description = message;
            this.FireDeviceEvent(evt);
        }
    }
    private void FireEvent(BLEEventTypes eventType, string message = null)
    {
        using (BLEEventArgs evt = new BLEEventArgs(eventType))
        {
            evt.Description = message;
            this.FireDeviceEvent(evt);
        }
    }

    public override string ToString()
    {
        return this.Name;
    }

}
