
#nullable enable

using System;
using System.Collections.Generic;
using Android;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using SmartPot.Application.Core;
using Xamarin.Essentials;
using static Android.Bluetooth.BluetoothClass;
using FragmentTransaction = AndroidX.Fragment.App.FragmentTransaction;
using ImprovManager = SmartPot.Application.Core.ImprovManager;

namespace SmartPot.Application.Views.Presenters
{
    internal sealed class DeviceListFragmentPresenter
    {
        private readonly Context context;
        private readonly List<ImprovDevice> devices;
        private MainActivity? mainActivity;
        private ImprovManager? improvManager;
        private SwipeRefreshLayout? layout;
        private RecyclerView? devicesList;
        private DeviceListAdapter? adapter;
        private IMenuItem? scanningMenuItem;
        private IMenuItem? stopScanningMenuItem;

        public DeviceListFragmentPresenter(Context context)
        {
            this.context = context;
            devices = new List<ImprovDevice>();
        }

        public void AttachView(View? view)
        {
            mainActivity = (MainActivity)Platform.CurrentActivity;
            improvManager = mainActivity.ImprovManager;
            layout = view?.FindViewById<SwipeRefreshLayout>(Resource.Id.swipe_refresh_layout);
            devicesList = view?.FindViewById<RecyclerView>(Resource.Id.devices_list);

            if (null != layout)
            {
                layout.SetOnRefreshListener(new RefreshListener(OnRefreshCallback));
            }

            if (null != devicesList)
            {
                adapter = new DeviceListAdapter(OnItemClick);
                devicesList.SetAdapter(adapter);
                devicesList.AddItemDecoration(new OffsetItemDecorator(16, 8));
                devicesList.AddItemDecoration(new DividerItemDecoration(context, 0));
            }

            if (null != improvManager)
            {
                improvManager.ScanStateChanged += OnScanStateChanged;
                improvManager.FoundDevice += OnFoundDevice;
            }
        }

        public void CreateOptionsMenu(IMenu? menu, MenuInflater inflater)
        {
            if (null != mainActivity)
            {
                inflater.Inflate(Resource.Menu.menu_main, menu);

                scanningMenuItem = menu?.FindItem(Resource.Id.action_start_scanning);
                stopScanningMenuItem = menu?.FindItem(Resource.Id.action_stop_scanning);

                if (null != stopScanningMenuItem)
                {
                    stopScanningMenuItem.SetVisible(false);
                }
            }
        }
        
        public bool OptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_start_scanning:
                {
                    if (false == improvManager.IsScanning)
                    {
                        OnRefreshCallback();
                    }

                    return true;
                }

                case Resource.Id.action_stop_scanning:
                {
                    StopScanning();
                    return true;
                }
            }

            return false;
        }

        private void ScanDevices()
        {
            improvManager.FindDevices();
        }

        private void StopScanning()
        {
            improvManager.StopScanning();
        }

        private void OnRefreshCallback()
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
                MainActivity.LocationPermissionRequest
            );
        }

        private void OnScanStateChanged(object sender, EventArgs e)
        {
            var scanning = improvManager?.IsScanning ?? false;

            System.Diagnostics.Debug.WriteLine($"Scanning state changed: {scanning}");

            if (null != layout)
            {
                layout.Refreshing = scanning;
            }

            scanningMenuItem?.SetVisible(false == scanning);
            stopScanningMenuItem?.SetVisible(scanning);
        }

        private void OnFoundDevice(object sender, FoundDeviceEventArgs e)
        {
            devices.Add(e.Device);

            if (null != adapter)
            {
                adapter.SubmitList(improvManager?.Devices!);
            }
        }

        private void OnItemClick(ImprovDevice device)
        {
            var fragment = ImprovDeviceFragment.NewInstance(device.Address, device.Name);
            var transaction = mainActivity?.SupportFragmentManager
                .BeginTransaction()
                .SetTransition(FragmentTransaction.TransitFragmentOpen)
                .AddToBackStack("ImprovDevice")
                .Replace(Resource.Id.navigation_container, fragment);

            if (null != improvManager)
            {
                improvManager.ScanStateChanged -= OnScanStateChanged;
                improvManager.FoundDevice -= OnFoundDevice;
            }

            transaction?.Commit();
        }

        #region RefreshListener

        private sealed class RefreshListener : Java.Lang.Object, SwipeRefreshLayout.IOnRefreshListener
        {
            private readonly Action onRefreshCallback;

            public RefreshListener(Action onRefreshCallback)
            {
                this.onRefreshCallback = onRefreshCallback;
            }

            void SwipeRefreshLayout.IOnRefreshListener.OnRefresh() => onRefreshCallback.Invoke();
        }

        #endregion

        #region DeviceListAdapter

        private sealed class ImprovDeviceItemViewHolder : RecyclerView.ViewHolder
        {
            private readonly Action<ImprovDevice> itemClickCallback;
            private readonly TextView? nameTextView;
            private readonly TextView? addressTextView;
            private ImprovDevice? device;

            public ImprovDevice? ImprovDevice
            {
                get => device;
                set
                {
                    device = value;

                    if (null != nameTextView)
                    {
                        nameTextView.Text = value?.Name ?? "UNKNOWN DEVICE";
                    }

                    if (null != addressTextView)
                    {
                        addressTextView.Text = value?.Address ?? "UNDEFINED";
                    }
                }
            }

            public ImprovDeviceItemViewHolder(View? itemView, Action<ImprovDevice> itemClickCallback)
                : base(itemView)
            {
                this.itemClickCallback = itemClickCallback;

                nameTextView = itemView?.FindViewById<TextView>(Resource.Id.item_device_name);
                addressTextView = itemView?.FindViewById<TextView>(Resource.Id.item_device_address);

                itemView?.SetOnClickListener(new ClickListener(OnClick));
            }

            private void OnClick(View? view) => itemClickCallback.Invoke(device!);
        }

        private sealed class DeviceListAdapter : ListAdapter
        {
            private readonly Action<ImprovDevice> itemClickCallback;

            public DeviceListAdapter(Action<ImprovDevice> itemClickCallback)
                : base(new DeviceItemCallback())
            {
                this.itemClickCallback = itemClickCallback;
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                if (holder is ImprovDeviceItemViewHolder deviceView)
                {
                    if (GetItem(position) is ImprovDevice improvDevice)
                    {
                        deviceView.ImprovDevice = improvDevice;
                    }
                }
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                var inflater = LayoutInflater.From(parent.Context);
                var view = inflater?.Inflate(Resource.Layout.layout_discovered_device_item, parent, false);
                return new ImprovDeviceItemViewHolder(view, itemClickCallback);
            }
        }

        private sealed class DeviceItemCallback : DiffUtil.ItemCallback
        {
            public override bool AreContentsTheSame(Java.Lang.Object p0, Java.Lang.Object p1)
            {
                var i0 = p0.JavaCast<ImprovDevice>();
                return i0.Equals(p1.JavaCast<ImprovDevice>());
            }

            public override bool AreItemsTheSame(Java.Lang.Object p0, Java.Lang.Object p1)
            {
                var equals = p0.Equals(p1);
                return equals;
            }
        }

        #endregion

        #region OffsetItemDecorator

        private sealed class OffsetItemDecorator : RecyclerView.ItemDecoration
        {
            private readonly int left;
            private readonly int top;
            private readonly int right;
            private readonly int bottom;

            public OffsetItemDecorator(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            public OffsetItemDecorator(int horizontal, int vertical)
                : this(horizontal, vertical, horizontal, vertical)
            {
            }

            public OffsetItemDecorator(int all)
                : this(all, all, all, all)
            {
            }

            public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
            {
                base.GetItemOffsets(outRect, view, parent, state);

                var position = parent.GetChildAdapterPosition(view);
                var layoutManager = parent.GetLayoutManager();

                if (layoutManager is LinearLayoutManager linearLayout)
                {
                    switch (linearLayout.Orientation)
                    {
                        case LinearLayoutManager.Vertical:
                            {
                                outRect.Left = left;
                                outRect.Top = top;
                                outRect.Right = right;
                                outRect.Bottom = bottom;

                                break;
                            }

                        case LinearLayoutManager.Horizontal:
                            {
                                outRect.Left = left;
                                outRect.Top = top;
                                outRect.Right = right;
                                outRect.Bottom = bottom;

                                break;
                            }
                    }
                }
            }
        }

        #endregion
    }
}

#nullable restore