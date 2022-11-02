
#nullable enable

using Android.Content;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.Snackbar;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace SmartPot.Application.Views.Presenters
{
    internal sealed class MainActivityPresenter
    {
        private readonly Context context;
        //private readonly ImprovManager improvManager;
        //private readonly List<ImprovDevice> devices;
        private MainActivity? mainActivity;
        private Toolbar? toolbar;
        //private SwipeRefreshLayout? layout;
        //private RecyclerView? devicesList;
        //private DeviceListAdapter? adapter;
        //private IMenuItem? stopScanning;

        public MainActivityPresenter(Context context)
        {
            /*var callback = new ImprovCallback
            {
                ScanningStateChanged = OnScanningStateChanged,
                ConnectionStateChanged = OnConnectionStateChanged,
                StateChange = OnStateChange,
                DeviceFound = OnDeviceFound
            };*/

            this.context = context;

            //devices = new List<ImprovDevice>();
            //improvManager = new ImprovManager(context, callback);
        }

        public void AttachView(MainActivity activity)
        {
            mainActivity = activity;

            activity.SetContentView(Resource.Layout.activity_main);

            toolbar = activity.FindViewById<Toolbar>(Resource.Id.toolbar);
            //layout = activity.FindViewById<SwipeRefreshLayout>(Resource.Id.swipe_refresh_layout);
            //devicesList = activity.FindViewById<RecyclerView>(Resource.Id.devices_list);

            if (null != toolbar)
            {
                activity.SetSupportActionBar(toolbar);
            }

            /*if (null != layout)
            {
                layout.SetOnRefreshListener(new RefreshListener(OnRefreshCallback));
            }

            if (null != devicesList)
            {
                adapter = new DeviceListAdapter(OnItemClick);
                devicesList.SetAdapter(adapter);
                devicesList.AddItemDecoration(new OffsetItemDecorator(16, 8));
                devicesList.AddItemDecoration(new DividerItemDecoration(context, 0));
            }*/
        }
        
        /*public bool CreateOptionsMenu(IMenu? menu)
        {
            if (null != mainActivity)
            {
                mainActivity.MenuInflater.Inflate(Resource.Menu.menu_main, menu);
                stopScanning = menu?.FindItem(Resource.Id.action_stop_scanning);

                if (null != stopScanning)
                {
                    stopScanning.SetVisible(false);
                }
            }

            return true;
        }*/

        /*public bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_stop_scanning:
                {
                    StopScanning();
                    return true;
                }
            }

            return false;
        }*/


        public void DetachView()
        {
            mainActivity = null;
        }

        public void ShowPermissionDeniedMessage()
        {
            var layout = mainActivity?.FindViewById<CoordinatorLayout>(Resource.Id.layout_main);
            Snackbar.Make(layout, Resource.String.message_permissions_denied, 10000);
        }

        /*private void OnRefreshCallback()
        {
            var permission = ContextCompat.CheckSelfPermission(context, Manifest.Permission.AccessFineLocation);
            
            if (Permission.Granted == permission)
            {
                ScanDevices();
            }

            ActivityCompat.RequestPermissions(
                mainActivity,
                new[]
                {
                    Manifest.Permission.AccessCoarseLocation,
                    Manifest.Permission.AccessFineLocation
                },
                LocationPermissionRequest
            );
        }*/

        /*public void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            var allGranted = Array.TrueForAll(grantResults, permission => permission == Permission.Granted);

            if (LocationPermissionRequest == requestCode && allGranted)
            {
                ScanDevices();
            }
            else
            {
                var layout = mainActivity?.FindViewById<CoordinatorLayout>(Resource.Id.layout_main);
                Snackbar.Make(layout, Resource.String.message_permissions_denied, 10000);
            }
        }*/

        /*private void ScanDevices()
        {
            improvManager.FindDevices();

            if (null != stopScanning)
            {
                stopScanning.SetVisible(true);
            }
        }

        private void StopScanning()
        {
            improvManager.StopScanning();

            if (null != stopScanning)
            {
                stopScanning.SetVisible(false);
            }
        }*/






    }
}

#nullable restore