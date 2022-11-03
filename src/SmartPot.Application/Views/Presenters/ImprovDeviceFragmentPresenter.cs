
#nullable enable

using Android.Views;
using Android.Widget;
using SmartPot.Application.Core;
using SmartPot.Application.Extensions;
using System;
using Android.App;
using Xamarin.Essentials;
using ImprovManager = SmartPot.Application.Core.ImprovManager;

namespace SmartPot.Application.Views.Presenters
{
    public class ImprovDeviceFragmentPresenter : CredentialsDialog.IDialogResultListener
    {
        private ImprovDevice? improvDevice;
        private MainActivity? parentActivity;
        private ImprovManager? improvManager;
        private TextView? currentStateTextView;
        private TextView? errorCodeTextView;
        private IMenuItem? connectMenuItem;
        private IMenuItem? disconnectMenuItem;

        public void SetDeviceAddress(string? value)
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

            if (null != improvDevice)
            {
                improvDevice.ConnectStateChanged += OnConnectStateChanged;
            }
        }

        public void AttachView(View view)
        {
            parentActivity = (MainActivity)Platform.CurrentActivity;
            improvManager = parentActivity.ImprovManager;
            currentStateTextView = view.FindViewById<TextView>(Resource.Id.device_current_state);
            errorCodeTextView = view.FindViewById<TextView>(Resource.Id.device_error_code);
            //improvManager.AddCallback(this);
        }

        public void DetachView()
        {
            //improvManager?.RemoveCallback(this);
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

        private void OnConnectStateChanged(object sender, EventArgs e)
        {
            MainThread
                .InvokeOnMainThreadAsync(() =>
                {
                    var isConnected = improvDevice?.IsConnected ?? false;

                    connectMenuItem?.SetVisible(false == isConnected);
                    disconnectMenuItem?.SetVisible(isConnected);

                    if (isConnected)
                    {
                        if (null != currentStateTextView)
                        {
                            currentStateTextView.Text = "Connected";
                        }

                        var dialog = CredentialsDialog.NewInstance();

                        dialog.AddDialogResultListener(this);
                        dialog.Show(parentActivity!.SupportFragmentManager, null);
                    }
                    else
                    {
                        if (null != currentStateTextView)
                        {
                            currentStateTextView.Text = "Not Connected";
                        }
                    }
                })
                .FireAndForget();
        }

        #region IDialogResultListener

        void CredentialsDialog.IDialogResultListener.OnSuccess(Dialog dialog, string ssid, string? password)
        {
            dialog.Dismiss();
            improvDevice?.SendCredentials(ssid, password);
        }

        void CredentialsDialog.IDialogResultListener.OnDismiss(Dialog dialog)
        {
            dialog.Dismiss();
        }

        #endregion
    }
}

#nullable disable