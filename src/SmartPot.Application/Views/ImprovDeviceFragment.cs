
#nullable enable

using Android.OS;
using Android.Views;
using SmartPot.Application.Views.Presenters;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace SmartPot.Application.Views
{
    public class ImprovDeviceFragment : Fragment
    {
        private const string DeviceAddressKey = "Device.Address";
        private const string DeviceNameKey = "Device.Name";

        private ImprovDeviceFragmentPresenter? presenter;

        public string? DeviceAddress
        {
            get
            {
                var address = Arguments.GetString(DeviceAddressKey);
                return address;
            }
        }

        public string? DeviceName
        {
            get
            {
                var name = Arguments.GetString(DeviceNameKey);
                return name;
            }
        }

        public static ImprovDeviceFragment NewInstance(string deviceAddress, string? deviceName)
        {
            var bundle = new Bundle();
            
            bundle.PutString(DeviceAddressKey, deviceAddress);
            bundle.PutString(DeviceNameKey, deviceName);

            return new ImprovDeviceFragment
            {
                Arguments = bundle
            };
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            HasOptionsMenu = true;
            base.OnCreate(savedInstanceState);
            presenter = new ImprovDeviceFragmentPresenter();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_improv_device, container, false);

            if (null != view)
            {
                if (null != presenter)
                {
                    presenter.AttachView(view);
                    presenter.SetDeviceAddress(DeviceAddress);
                }

                return view;
            }

            return base.OnCreateView(inflater, container, savedInstanceState);
        }

        public override void OnCreateOptionsMenu(IMenu? menu, MenuInflater inflater)
        {
            presenter?.CreateOptionsMenu(menu, inflater);
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();
            presenter?.DetachView();
            presenter = null;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return presenter?.OptionsItemSelected(item) ?? base.OnOptionsItemSelected(item);
        }
    }
}

#nullable restore