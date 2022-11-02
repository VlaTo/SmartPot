
#nullable enable

using Android.OS;
using Android.Util;
using Android.Views;
using AndroidX.AppCompat.App;
using SmartPot.Application.Views.Presenters;

namespace SmartPot.Application.Views
{
    /// <summary>
    /// 
    /// </summary>
    public class CredentialsDialog : AppCompatDialogFragment
    {
        private CredentialsDialogPresenter? presenter;
        private IResultListener? resultListener;

        public interface IResultListener
        {
            void OnSend(string ssid, string? password);

            void OnDismiss();
        }

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

                if (null != resultListener)
                {
                    presenter?.SetResultListener(resultListener);
                }

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
                var width = displayMetrics.WidthPixels - (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 52.0f, displayMetrics);
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

        public CredentialsDialog SetResultListener(IResultListener listener)
        {
            if (null != presenter)
            {
                presenter.SetResultListener(listener);
            }
            else
            {
                resultListener = listener;
            }

            return this;
        }
    }
}

#nullable restore
