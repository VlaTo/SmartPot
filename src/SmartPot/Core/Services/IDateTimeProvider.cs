using SmartPot.Core.UI;
using System;

namespace SmartPot.Core.Services
{
    internal interface IDateTimeProvider : IUpdateable
    {
        DateTime Now
        {
            get;
        }

        public void Initialize();
    }
}
