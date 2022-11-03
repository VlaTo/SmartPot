
#nullable enable

using Android.App;
using Android.OS;
using Android.Util;
using Android.Views;
using AndroidX.AppCompat.App;
using SmartPot.Application.Views.Presenters;
using static SmartPot.Application.Views.Presenters.CredentialsDialogPresenter;

namespace SmartPot.Application.Views
{
    /// <summary>
    /// 
    /// </summary>
    public class CredentialsDialog : AppCompatDialogFragment, IActionCallback
    {
        private CredentialsDialogPresenter? presenter;
        private IDialogResultListener? resultListener;

        #region IDialogResultListener

        public interface IDialogResultListener
        {
            void OnSuccess(Dialog dialog, string ssid, string? password);

            void OnDismiss(Dialog dialog);
        }

        #endregion

        public static CredentialsDialog NewInstance()
        {
            var bundle = new Bundle();
            return new CredentialsDialog
            {
                Arguments = bundle
            };
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            presenter = new CredentialsDialogPresenter();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            var view = inflater.Inflate(Resource.Layout.layout_dialog_credentials, container, false);

            if (null != view)
            {
                presenter?.AttachView(view);
                presenter?.SetActionCallbacks(this);

                return view;
            }

            return base.OnCreateView(inflater, container, savedInstanceState);
        }

        public override void OnStart()
        {
            base.OnStart();

            var window = Dialog.Window;

            if (null == window)
            {
                return;
            }

            var displayMetrics = Android.App.Application.Context.Resources?.DisplayMetrics;

            if (null != displayMetrics)
            {
                var width = displayMetrics.WidthPixels -
                            (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 52.0f, displayMetrics);
                //var height = displayMetrics.HeightPixels - (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 104.0f, displayMetrics);
                var layoutParams = window.Attributes;

                if (null != layoutParams)
                {
                    layoutParams.Width = width;
                    //layoutParams.Height = height;

                    window.Attributes = layoutParams;
                }
            }
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();
            presenter?.DetachView();
            presenter = null;
        }

        public void AddDialogResultListener(IDialogResultListener listener)
        {
            resultListener = listener;
        }

        #region IActionCallback

        void IActionCallback.OnAction(DialogAction action)
        {
            if (null != resultListener)
            {
                switch (action)
                {
                    case DialogAction.Positive:
                    {
                        var ssid = presenter?.Ssid;
                        var password = presenter?.Password;

                        resultListener.OnSuccess(Dialog, ssid!, password);

                        break;
                    }

                    case DialogAction.Negative:
                    {
                        resultListener.OnDismiss(Dialog);

                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
            }
        }

        #endregion
    }
}

#nullable restore
