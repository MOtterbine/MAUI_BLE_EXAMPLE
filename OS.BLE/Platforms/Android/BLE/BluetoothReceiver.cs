using Android.Bluetooth;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OS.BLE;

public class BluetoothReceiver : BroadcastReceiver
{
    private void FireEnabled()
    {
        if (BluetoothEnabled == null) return;
        BluetoothEnabled(this, EventArgs.Empty);
    }
    public event EventHandler BluetoothEnabled;

    private void FireDisabled()
    {
        if (BluetoothDisabled == null) return;
        BluetoothDisabled(this, EventArgs.Empty);
    }
    public event EventHandler BluetoothDisabled;

    private byte enabled = 0x00;
    private byte disabled = 0x00;

    public override void OnReceive(Context context, Intent intent)
    {
        
        if (BluetoothAdapter.ActionStateChanged.Equals(intent.Action))
        {
            var state = intent.GetIntExtra(BluetoothAdapter.ExtraState, -1);
            switch (state)
            {
                case 0x0B: // Turning on
                    enabled |= 0x01;
                    if (enabled >= 0x03)
                    {
                        enabled = 0x00;
                        FireEnabled();
                    }
                    break;
                case 0x0C: // On and Enabled
                    enabled |= 0x02;
                    if (enabled >= 0x03)
                    {
                        enabled = 0x00;
                        FireEnabled();
                    }
                    break;
                case 0x0D: // Turning Off
                    disabled |= 0x01;
                    if (disabled >= 0x03)
                    {
                        disabled = 0x00;
                        FireDisabled();
                    }
                    break;
                case 0x0A: // Off and Diisabled
                    disabled |= 0x02;
                    if (disabled >= 0x03)
                    {
                        disabled = 0x00;
                        FireDisabled();
                    }
                    break;
            }


        }
    }
}