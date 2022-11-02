
#nullable enable

using System;
using Android.Views;

namespace SmartPot.Application.Core
{
    internal sealed class ClickListener : Java.Lang.Object, View.IOnClickListener
    {
        private readonly Action<View?> action;

        public ClickListener(Action<View?> action)
        {
            this.action = action;
        }

        public void OnClick(View? view) => action.Invoke(view);
    }
}

#nullable restore