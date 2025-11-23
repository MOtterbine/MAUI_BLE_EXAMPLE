using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MAUI_BLE_EXAMPLE.Behaviors
{
    public static class PercentValidationBehavior
    {
        public static readonly BindableProperty AttachBehaviorProperty =
            BindableProperty.CreateAttached(
                "AttachBehavior",
                typeof(bool),
                typeof(PercentValidationBehavior),
                false,
                propertyChanged: OnAttachBehaviorChanged);

        public static bool GetAttachBehavior(BindableObject view)
        {
            return (bool)view.GetValue(AttachBehaviorProperty);
        }

        public static void SetAttachBehavior(BindableObject view, bool value)
        {
            view.SetValue(AttachBehaviorProperty, value);
        }

        static void OnAttachBehaviorChanged(BindableObject view, object oldValue, object newValue)
        {
            var entry = view as Entry;
            if (entry == null)
            {
                return;
            }

            bool attachBehavior = (bool)newValue;
            if (attachBehavior)
            {
                entry.TextChanged += OnEntryTextChanged;
            }
            else
            {
                entry.TextChanged -= OnEntryTextChanged;
            }
        }

        static void OnEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.NewTextValue)) return;


            // looking to match anything not in the string
            string sPattern = "^[0-9]*$";
            bool isValid = Regex.IsMatch(args.NewTextValue, sPattern);
            if(int.TryParse(args.NewTextValue, out int val))
            {
                if (val > 100)
                {
                    ((Entry)sender).Text = "100";
                }
            }
            else
            {
                isValid = false;
            }
            if (!isValid)
            {
                ((Entry)sender).Text = args.OldTextValue;
            }

        }
    }
}
