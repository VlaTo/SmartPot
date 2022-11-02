
#nullable enable

using System;
using System.Diagnostics;
using Android.Views;
using Android.Widget;
using SmartPot.Application.Core;
using SmartPot.Application.Extensions;
using Xamarin.Essentials;
using ImprovManager = SmartPot.Application.Core.ImprovManager;

namespace SmartPot.Application.Views.Presenters
{
    public class ImprovDeviceFragmentPresenter : ImprovManager.ICallback, CredentialsDialog.IResultListener
    {
        private ImprovDevice? improvDevice;
        private MainActivity? parentActivity;
        private ImprovManager? improvManager;
        private TextView? currentStateTextView;
        private TextView? errorCodeTextView;
        private IMenuItem? connectMenuItem;
        private IMenuItem? disconnectMenuItem;
        private CredentialsDialog? dialog;

        public void SetDeviceAddress(string value)
        {
            if (null != improvManager)
            {
                improvDevice = Array.Find(improvManager.Devices, device => String.Equals(device.Address, value));
            }

            var actionBarTitle = improvDevice?.Name ?? "UNKNOWN";

            if (null != parentActivity)
            {
                parentActivity.SupportActionBar.Title = actionBarTitle;
            }
        }

        public void AttachView(View view)
        {
            parentActivity = (MainActivity)Platform.CurrentActivity;
            improvManager = parentActivity.ImprovManager;
            currentStateTextView = view.FindViewById<TextView>(Resource.Id.device_current_state);
            errorCodeTextView = view.FindViewById<TextView>(Resource.Id.device_error_code);
            improvManager.AddCallback(this);
        }

        public void DetachView()
        {
            improvManager?.RemoveCallback(this);
        }

        public void CreateOptionsMenu(IMenu? menu, MenuInflater inflater)
        {
            if (null == parentActivity)
            {
                return;
            }

            inflater.Inflate(Resource.Menu.menu_device, menu);

            connectMenuItem = menu?.FindItem(Resource.Id.action_connect);
            disconnectMenuItem = menu?.FindItem(Resource.Id.action_disconnect);

            if (null != disconnectMenuItem)
            {
                disconnectMenuItem.SetVisible(false);
            }
        }

        public bool OptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_connect:
                {
                    connectMenuItem?.SetVisible(false);
                    disconnectMenuItem?.SetVisible(true);

                    improvDevice?.Connect();

                    return true;
                }

                case Resource.Id.action_disconnect:
                {
                    connectMenuItem?.SetVisible(true);
                    disconnectMenuItem?.SetVisible(false);

                    return true;
                }
            }

            return false;
        }

        #region ICallback

        void ImprovManager.ICallback.OnScanningStateChanged(bool scanning)
        {
        }

        void ImprovManager.ICallback.OnDeviceFound(ImprovDevice device)
        {
            ;
        }

        #endregion

        #region IResultListener

        void CredentialsDialog.IResultListener.OnSend(string ssid, string? password)
        {
            dialog?.Dismiss();
            improvDevice?.SendCredentials(ssid, password);
        }

        void CredentialsDialog.IResultListener.OnDismiss()
        {
            ;
        }

        #endregion

        #region ImprovDevice callback

        /*void ImprovDevice.IImprovCallback.OnConnected(bool connected)
        {
            MainThread
                .InvokeOnMainThreadAsync(() =>
                {
                    connectMenuItem?.SetVisible(false == connected);
                    disconnectMenuItem?.SetVisible(connected);

                    if (connected)
                    {
                        if (null != currentStateTextView)
                        {
                            currentStateTextView.Text = "Connected";
                        }

                        dialog = CredentialsDialog
                            .NewInstance()
                            .SetResultListener(this);

                        dialog.Show(parentActivity!.SupportFragmentManager, null);
                    }
                    else
                    {
                        ;
                    }
                })
                .FireAndForget();
        }*/

        /*void ImprovDevice.IImprovCallback.OnCredentialsSent()
        {
            if (null != currentStateTextView)
            {
                currentStateTextView.Text = "Sent";
            }
        }*/

        #endregion
    }
}

#nullable disable