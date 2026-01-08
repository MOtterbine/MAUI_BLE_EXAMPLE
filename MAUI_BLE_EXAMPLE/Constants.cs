
namespace MAUI_BLE_EXAMPLE;

public class Constants 
{

    public const int DEFAULT_COMM_NO_RESPONSE_TIMEOUT = 5000;
    public const int MIN_WINDOW_HEIGHT_WINDOWS = 680;
    public const int MIN_WINDOW_WIDTH_WINDOWS = 500;

    // Main Service Guid
    public const string UUID_BLE_SERVICE_UUID = "C1B24045-41AF-425A-97CC-758CB1FAEB08";
    // Main Service Characteristics
    public const string UUID_BLE_CHARACTERISTIC_COMMAND = "E7D73222-8BDF-49B5-BAFE-287E72D351A8";
    public const string UUID_BLE_CHARACTERISTIC_LEDPWM = "2E46BE34-77C5-42D5-B40F-AE9F487DD1E6";
    public const string UUID_BLE_CHARACTERISTIC_BUTTON = "65928536-7969-4255-B57D-FBD7E36F1363";
    public const string UUID_BLE_CHARACTERISTIC_TEMPERATURE = "0883F6A5-20F9-4735-B658-EC39C77E82D8";
    public const string UUID_BLE_CHARACTERISTIC_MESSAGE = "68F5676B-6203-4A03-AC76-733D251062E2";

    public const string STRING_EMPTY_FIELD_INDICATOR = "-----";
    public const string STRING_BUTTON_LABEL_ON = "\x1B75";
    public const string STRING_BUTTON_LABEL_OFF = "\x058D";

    public const string STRING_NA = "N/A";
    public const string STRING_UNKNOWN_ERROR = "An Error occurred";
    public const string STRING_DISCONNECTED = "Disconnected";
    public const string STRING_CONNECTED = "*** Connected ***";
    public const string STRING_CONNECTING = "Connecting";
    public const string STRING_STARTING = "Starting...";
    public const string STRING_SCANNING_DEVICES = "Scanning...";
    public const string STRING_DISCOVERING_SERVICES = "Discovering Services...";
    public const string STRING_NONE = "<None>";


}
