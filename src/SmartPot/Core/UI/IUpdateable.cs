using System;

namespace SmartPot.Core.UI
{
    internal interface IUpdateable
    {
        void Update(TimeSpan elapsed);
    }
}
