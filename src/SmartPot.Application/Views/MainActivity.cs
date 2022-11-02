
#nullable enable

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using SmartPot.Application.Views.Presenters;
using System;
using FragmentTransaction = AndroidX.Fragment.App.FragmentTransaction;
using ImprovManager = SmartPot.Application.Core.ImprovManager;

namespace SmartPot.Application.Views
{
    /// <summary>
    /// Main Activity
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        public const int LocationPermissionRequest = 102;

        private readonly MainActivityPresenter presenter;

        internal ImprovManager ImprovManager
        {
            get;
        }

        public MainActivity()
        {
            presenter = new MainActivityPresenter(Android.App.Application.Context);
            ImprovManager = new ImprovManager(Android.App.Application.Context);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            presenter.AttachView(this);

            var transaction = SupportFragmentManager
                .BeginTransaction()
                .SetTransition(FragmentTransaction.TransitFragmentOpen)
                .Replace(Resource.Id.navigation_container, DeviceListFragment.NewInstance())
                .Commit();
        }

        protected override void OnDestroy()
        {
            presenter.DetachView();

            base.OnDestroy();
        }

        //public override bool OnCreateOptionsMenu(IMenu menu) => presenter.CreateOptionsMenu(menu);

        //public override bool OnOptionsItemSelected(IMenuItem item)
        //    => presenter.OnOptionsItemSelected(item) || base.OnOptionsItemSelected(item);


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            var allGranted = Array.TrueForAll(grantResults, permission => permission == Permission.Granted);

            if (LocationPermissionRequest == requestCode && allGranted)
            {
                //Message
                //ScanDevices();
            }
            else
            {
                presenter.ShowPermissionDeniedMessage();
            }
        }
    }
}

#nullable restore