namespace OS.BLE;

public enum BLEEventTypes
{
    ScanResultsReceived,
    ServicesDiscovered,
    RemoteDataReady,
    ConnectedAsClient,
    ClientConnected,
    RemoteDisconnect,
    Disconnected,
    Scanning,
    Transmit,
    Receive,
    Listening,
    Connecting,
    LinkInitFailure,
    LinkInitSuccess,
    Information,
    Error
}
