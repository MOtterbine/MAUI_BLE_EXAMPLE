using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using OS.BLE;

#if WINDOWS || ANDROID
using CommunityToolkit.Maui.Views;

#endif

#if WINDOWS
using MAUI_BLE_EXAMPLE.WinUI;
#endif

namespace MAUI_BLE_EXAMPLE;

public partial class BLEPage : ContentPage
{
    public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    readonly App pApp = ((App)App.Current);
    IMyBLEControl _Esp32SuperMini;

    public BLEPage()
    {

        InitializeComponent();
#if WINDOWS
        //App.WindowInstance.TitleBar = new Microsoft.Maui.Controls.TitleBar
        //{
        //    Icon = "titlebar_icon.png",
        //    Title = "My App",
        //    Subtitle = "Demo",
        ////    Content = "Eat Shit"
        //};
#endif
        CanSend = false;
        
        // Create CODE-BEHIND Bindings
        _ = new Binding("StatusMessage") { Source = this };
        _ = new Binding("CanSend") { Source = this };
        _ = new Binding("DeviceMessage") { Source = this };
        _ = new Binding("DeviceName") { Source = this };
        _ = new Binding("OnEnabled") { Source = this };
        _ = new Binding("OffEnabled") { Source = this };
        _ = new Binding("LEDValue") { Source = this };
        _ = new Binding("Version") { Source = this };
        _ = new Binding("ButtonValue") { Source = this };
        _ = new Binding("LEDIsOn") { Source = this };
        _ = new Binding("ToggleButtonText") { Source = this };
        _ = new Binding("PWM") { Source = this };

        //Bind the xaml to this class
        BindingContext = this;

        StatusMessage = Constants.STRING_SCANNING_DEVICES;

        if (pApp.HasPermissions)
        {
            _Esp32SuperMini = new MAUI_BLEDevice() as IMyBLEControl;

            if (_Esp32SuperMini == null) throw new NullReferenceException("Could not instantiate the basic code framework for MAUI_BLEDevice object as IMP3Player.");
            _Esp32SuperMini.BleEvent += OnBLEEvent;
            _Esp32SuperMini.StartScan();
        }
        else
        {
            // no permissions yet, so listen for them
            pApp.PermissionsReady += PApp_PermissionsReadyEvent;
            pApp.Sleep += OnAppSleep;
            pApp.Resume += OnAppResume;
        }

        pApp.AppClosing += OnAppClosing;

    }

    private void OnAppClosing(object? sender, EventArgs e)
    {
#if WINDOWS
        pApp.Dispatcher.Dispatch(() => {
            //var ctx = Application.Current?.MainPage?.Handler?.MauiContext;
            //pApp.MainPage.SetCustomCursor(CursorIcon.Wait, ctx);
        });
#endif 

        this._Esp32SuperMini?.Disconnect();
    }

    private void OnAppResume(object? sender, EventArgs e)
    {
        this._Esp32SuperMini?.StartScan();
    }

    private void OnAppSleep(object? sender, EventArgs e)
    {
        
        this._Esp32SuperMini?.Disconnect();
    }

    private void ResetFields()
    {

        this.Temperature = 0;
        this.DeviceMessage = string.Empty;
        this.ButtonValue = 0;
        this.Temperature = 0x00;
        this.PWM = 0x00;
      
        UpdateConrols();

    }

    private void UpdateConrols()
    {
        
        OnPropertyChanged(nameof(OnEnabled));
        OnPropertyChanged(nameof(OffEnabled));
        OnPropertyChanged(nameof(LEDValue));
        OnPropertyChanged(nameof(LEDIsOn));
        OnPropertyChanged(nameof(ToggleButtonText));
        OnPropertyChanged(nameof(DeviceName));
        OnPropertyChanged(nameof(PWM));

    }

    private async Task OnBLEEvent(object sender, BLEEventArgs e)
    {
        switch (e.Event)
        {
            case BLEEventTypes.Information:
                StatusMessage = e.data + "\n";
                break;
            case BLEEventTypes.ScanResultsReceived:
                StatusMessage = "Scan Complete\n";
                this._Esp32SuperMini.Connect();
                break;
            case BLEEventTypes.Connecting:
                ResetFields();
                StatusMessage = Constants.STRING_CONNECTING;
                break;
            case BLEEventTypes.ConnectedAsClient:
                StatusMessage = Constants.STRING_CONNECTED;
                CanSend = true;
                break;
            case BLEEventTypes.Disconnected:
                CanSend = false;
                StatusMessage = Constants.STRING_DISCONNECTED;
                break;
            case BLEEventTypes.Scanning:
                CanSend = false;
                StatusMessage = Constants.STRING_SCANNING_DEVICES;
                break;
            case BLEEventTypes.RemoteDisconnect:
                CanSend = false;
                StatusMessage = "Disconnecting...";
                break;
            case BLEEventTypes.ServicesDiscovered:
                CanSend = true;
                StatusMessage = "Services Discovered";
                break;
            case BLEEventTypes.RemoteDataReady:
                UpdateConrols();
                await Application.Current.Dispatcher.DispatchAsync(() =>
                {
                    try
                    {
                        System.Reflection.PropertyInfo propInf = this.GetType().GetProperty(e.Description);
                        propInf?.SetValue(this, e.data);
                    } catch { }
                });
                break;
            case BLEEventTypes.Error:
                await Dispatcher.DispatchAsync(() =>
                {
                    if (string.IsNullOrEmpty(e.Description))
                    {
                        StatusMessage = Constants.STRING_UNKNOWN_ERROR;
                    }
                    StatusMessage = e.Description;
                    this.CanSend = false;// _Esp32SuperMini.IsEnabled;
                    ResetFields();
                });

                break;

        }

    }


    private void PApp_PermissionsReadyEvent(object sender, EventArgs e)
    {
        if (!pApp.HasPermissions) return;


        _Esp32SuperMini = new MAUI_BLEDevice() as IMyBLEControl;

        if (_Esp32SuperMini == null) throw new NullReferenceException("Could not instantiate the basic code framework for MAUI_BLEDevice object as IBLEDevice.");
        _Esp32SuperMini.BleEvent += OnBLEEvent;
        _Esp32SuperMini.StartScan();

        OnPropertyChanged(nameof(CanSend));
    }

    public bool IsButtonPressed => !(_buttonValue == 0);

    public String DeviceName => _Esp32SuperMini == null ? Constants.STRING_NONE : _Esp32SuperMini.Name;

    public UInt16 PWM
    {
        get => _PWM;
        set
        {
            _PWM = value;
            OnPropertyChanged(nameof(PWM));
        }
    }
    private UInt16 _PWM =0;




    public String ToggleButtonText
    {
        get => _ToggleButtonText;
        set
        {
            _ToggleButtonText = value;
            OnPropertyChanged(nameof(ToggleButtonText));
        }
    }
    private String _ToggleButtonText = Constants.STRING_BUTTON_LABEL_OFF;



    public bool LEDIsOn => CanSend && LEDValue != 0;

    public int LEDValue
    {
        get => _LEDValue;
        set
        {
            _LEDValue = value;
            if (value == 0)
            {
                ToggleButtonText = Constants.STRING_BUTTON_LABEL_ON;
            }
            else
            {
                ToggleButtonText = Constants.STRING_BUTTON_LABEL_OFF;
            }

            OnPropertyChanged(nameof(LEDValue));
            OnPropertyChanged(nameof(OnEnabled));
            OnPropertyChanged(nameof(OffEnabled));
            OnPropertyChanged(nameof(LEDIsOn));

        }
    }
    private int _LEDValue = 0;


    public int ButtonValue
    {
        get => _buttonValue;
        set
        {
            _buttonValue = value;
            OnPropertyChanged(nameof(ButtonValue));
            OnPropertyChanged(nameof(IsButtonPressed));
            OnPropertyChanged(nameof(OnEnabled));
            OnPropertyChanged(nameof(OffEnabled));

        }
    }
    private int _buttonValue = 0;


    public double Temperature
    {
        get => _temperature;
        set
        {
            _temperature = value;
            OnPropertyChanged(nameof(Temperature));
        }
    }
    private double _temperature = 0.0f;

    public bool OnEnabled => CanSend && LEDValue > 0;
    public bool OffEnabled => CanSend && LEDValue == 0;


    public String StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }
    private string _statusMessage = string.Empty;
    public String DeviceMessage
    {
        get => _DeviceMessage;
        set
        {
            _DeviceMessage = value;
            OnPropertyChanged(nameof(DeviceMessage));
        }
    }
    private string _DeviceMessage = string.Empty;

    public bool CanSend 
    {
        get => _canSend && pApp.HasPermissions;
        set
        {
            // update the value and notify the xaml
            _canSend = value;
            OnPropertyChanged(nameof(CanSend));
            UpdateConrols();

        }
    }
    private bool _canSend = true;


    public string SendData { get; set; } = string.Empty;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

#if WINDOWS
        /*
            Because this is the first, default page to be shown, the controls must be instantiated
            before they can be assigned CursorIcon.Hand behavor - so it's like this...
            (otherwise, the Behavior can normally be set in xaml)
        */
        // SendButton.SetCustomCursor(CursorIcon.Hand, SendButton.Handler?.MauiContext);



#endif

    }

#if WINDOWS || ANDROID


    private async Task<bool> ShowPopup(PopupInfo popupInfo)
    {
        var popup = new OSPopup(popupInfo);
        var result = await this.ShowPopupAsync(popup);
        if (result is bool boolResult) return boolResult;
        return false;
    }


    protected override bool OnBackButtonPressed()
    {
        Task<bool> answer = ShowPopup(new PopupInfo("BLE Controller", $"{Environment.NewLine}Exit Application ?{Environment.NewLine}", true));
        answer.ContinueWith(task =>
        {

        if (task.Result) // true
        {

#if ANDROID
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.FinishAndRemoveTask();
                MainThread.BeginInvokeOnMainThread(() => {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                });
#endif

            }

        });

        return true;
    }

#endif



    private void OnBLEButtonClicked_on(object sender, EventArgs e)
    {
        Dispatcher.Dispatch(() => { 
            // StatusMessage = "Set LED on";
            this._Esp32SuperMini.SetLight(true);
        });
    }

    private void OnBLEButtonClicked_off(object sender, EventArgs e)
    {
        Dispatcher.Dispatch(() => {
       // StatusMessage = "Set LED off";
        this._Esp32SuperMini.SetLight(false);
        });
    }

    private void OnBLEButtonClicked_Toggle(object sender, EventArgs e)
    {
        if(LEDValue == 0)
        {
            this._Esp32SuperMini.SetLight(true);
            return;
        }
        
        this._Esp32SuperMini.SetLight(false);
    }

    private void OnBLEStartScanClicked(object sender, EventArgs e)
    {
        CanSend = false;
        this._Esp32SuperMini.StartScan();
        ResetFields();
        StatusMessage = Constants.STRING_SCANNING_DEVICES;

    }

    private void OnBLEStopScanClicked(object sender, EventArgs e)
    {
        CanSend = false;
        this._Esp32SuperMini.StopScan();
        StatusMessage = "...Stopping";
    }

    private void OnPWMCompleted(object sender, EventArgs e)
    {
        
        this._Esp32SuperMini.SetLightLevel(PWM);
        var en = sender as Entry;
        if (en == null) return;
        Application.Current.Dispatcher.DispatchAsync(() =>
        {
//#if WINDOWS // in android this will cause keyboard to keep reopening
            en.Focus();
            en.CursorPosition = 0;
           en.SelectionLength = 4; // length is limited in xaml
//#endif
        });
    }


}
