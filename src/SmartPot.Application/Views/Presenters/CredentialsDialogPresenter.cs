
#nullable enable

using Android.Views;
using Android.Widget;
using SmartPot.Application.Core;

namespace SmartPot.Application.Views.Presenters
{
    internal sealed class CredentialsDialogPresenter
    {
        private IActionCallback? actionCallback;
        private EditText? inputSsid;
        private EditText? inputPassword;
        private Button? sendButton;
        private Button? cancelButton;
        private string? ssid;
        private string? password;

        #region IActionCallback

        public enum DialogAction
        {
            Unknown = -1,
            Positive,
            Negative
        }

        public interface IActionCallback
        {
            void OnAction(DialogAction action);
        }

        #endregion

        public string? Ssid
        {
            get => null != inputSsid ? inputSsid.Text : ssid;
            set
            {
                ssid = value;

                if (null != inputSsid)
                {
                    inputSsid.Text = value;
                }
            }
        }

        public string? Password
        {
            get => null != inputPassword ? inputPassword.Text : password;
            set
            {
                password = value;

                if (null != inputPassword)
                {
                    inputPassword.Text = value;
                }
            }
        }

        public void AttachView(View view)
        {
            inputSsid = view.FindViewById<EditText>(Resource.Id.text_input_ssid);
            inputPassword = view.FindViewById<EditText>(Resource.Id.text_input_password);
            sendButton = view.FindViewById<Button>(Resource.Id.button_credentials_send);
            cancelButton = view.FindViewById<Button>(Resource.Id.button_cancel);

            if (null != inputSsid)
            {
                inputSsid.Text = ssid;
            }

            if (null != inputPassword)
            {
                inputPassword.Text = password;
            }

            if (null != sendButton)
            {
                sendButton.SetOnClickListener(new ClickListener(OnSendButton));
            }

            if (null != cancelButton)
            {
                cancelButton.SetOnClickListener(new ClickListener(OnCancelButton));
            }
        }

        public void DetachView()
        {
            ;
        }

        public void SetActionCallbacks(IActionCallback callback)
        {
            actionCallback = callback;
        }
        
        private void OnSendButton(View? _)
        {
            if (null != actionCallback)
            {
                actionCallback.OnAction(DialogAction.Positive);
            }
        }

        private void OnCancelButton(View? _)
        {
            if (null != actionCallback)
            {
                actionCallback.OnAction(DialogAction.Negative);
            }
        }
    }
}

#nullable restore