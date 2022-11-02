

#nullable enable

using Android.Views;
using Android.Widget;
using SmartPot.Application.Core;
using static SmartPot.Application.Views.CredentialsDialog;

namespace SmartPot.Application.Views.Presenters
{
    internal sealed class CredentialsDialogPresenter
    {
        private IResultListener? resultListener;
        private EditText? inputSsid;
        private EditText? inputPassword;
        private Button? sendButton;
        private Button? cancelButton;

        public CredentialsDialogPresenter()
        {
        }

        public void AttachView(View view)
        {
            inputSsid = view.FindViewById<EditText>(Resource.Id.text_input_ssid);
            inputPassword = view.FindViewById<EditText>(Resource.Id.text_input_password);
            sendButton = view.FindViewById<Button>(Resource.Id.button_credentials_send);
            cancelButton = view.FindViewById<Button>(Resource.Id.button_cancel);

            if (null != inputSsid)
            {

            }

            if (null != inputPassword)
            {

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

        public void SetResultListener(IResultListener listener)
        {
            resultListener = listener;
        }
        
        private void OnSendButton(View? _)
        {
            if (null == resultListener)
            {
                return;
            }

            var ssid = inputSsid?.Text;
            var password = inputPassword?.Text;
            
            resultListener.OnSend(ssid!, password);
        }

        private void OnCancelButton(View? _)
        {
            if (null == resultListener)
            {
                return;
            }

            resultListener.OnDismiss();
        }
    }
}

#nullable restore