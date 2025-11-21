using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if WINDOWS || ANDROID

using CommunityToolkit.Maui.Views;


namespace MAUI_BLE_EXAMPLE;

public partial class OSPopup : Popup////<bool>
{

    App pApp => ((App)Application.Current);
    public OSPopup(PopupInfo popupInfo)
    {
        InitializeComponent();

        BindingContext = this;
        // user must click button to dismiss dialog
        this.CanBeDismissedByTappingOutsideOfPopup = false;

        // PopupInfo bDat = new PopupInfo() { Title = "shit", Message = "shit sticken" };
        this.Title.Text = popupInfo.Title;
        this.MessageText.Text = popupInfo.Message;
        this.IsYesNo = popupInfo.IsYesNo;
        if (!String.IsNullOrEmpty(popupInfo.OkText)) this.OkText = popupInfo.OkText;
        if (!String.IsNullOrEmpty(popupInfo.CancelText)) this.CancelText = popupInfo.CancelText;

    }

    void Button_Clicked(object sender, EventArgs args)
    {
        // 'Dismiss' returns whatever we pass back...
        Close(true);
    }
    void Button_cancel_Clicked(object sender, EventArgs args)
    {
        // 'Dismiss' returns whatever we pass back...
        Close(false);
    }

    public bool IsYesNo
    {
        get => (bool)GetValue(IsYesNoProperty);
        set => SetValue(IsYesNoProperty, value);
    }
    public static readonly BindableProperty IsYesNoProperty =
        BindableProperty.Create("IsYesNo", typeof(bool), typeof(OSPopup), false);

    public string OkText
    {
        get => (string)GetValue(OkTextProperty);
        set => SetValue(OkTextProperty, value);
    }
    public static readonly BindableProperty OkTextProperty =
        BindableProperty.Create("OkText", typeof(string), typeof(OSPopup), "Ok");

    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }
    public static readonly BindableProperty CancelTextProperty =
        BindableProperty.Create("CancelText", typeof(string), typeof(OSPopup), "Cancel");

}

public class PopupInfo
{
    public PopupInfo(string title, string msg, bool isYesNo = false, string okText = null, string cancelText = null)
    {
        this.IsYesNo = isYesNo;
        if (String.IsNullOrEmpty(title)) throw (new ArgumentException());
        if (String.IsNullOrEmpty(msg)) throw (new ArgumentException());
        this.Title = title;
        this.Message = msg;
        OkText = okText;
        CancelText = cancelText;

    }
    public string Title;
    public string Message;
    public bool IsYesNo { get; set; }

    public string OkText { get; set; } = null;
    public string CancelText { get; set; } = null;

}

#endif