
#nullable enable

using Android.OS;
using Android.Views;
using SmartPot.Application.Views.Presenters;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace SmartPot.Application.Views
{
    /// <summary>
    /// 
    /// </summary>
    public class DeviceListFragment : Fragment
    {
        private DeviceListFragmentPresenter? presenter;

        public static DeviceListFragment NewInstance()
        {
            var bundle = new Bundle();
            var fragment = new DeviceListFragment
            {
                Arguments = bundle
            };
            return fragment;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            HasOptionsMenu = true;
            base.OnCreate(savedInstanceState);
            presenter = new DeviceListFragmentPresenter(Android.App.Application.Context);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_device_list, container, false);

            if (null != view)
            {
                presenter?.AttachView(view);

                return view;
            }

            return base.OnCreateView(inflater, container, savedInstanceState);
        }

        public override void OnCreateOptionsMenu(IMenu? menu, MenuInflater inflater)
        {
            presenter?.CreateOptionsMenu(menu, inflater);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return presenter?.OptionsItemSelected(item) ?? base.OnOptionsItemSelected(item);
        }
    }
}

#nullable restore