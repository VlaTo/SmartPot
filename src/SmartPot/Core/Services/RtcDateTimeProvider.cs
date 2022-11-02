using Iot.Device.Rtc;
using System;
using nanoFramework.Runtime.Native;

namespace SmartPot.Core.Services
{
    internal sealed class RtcDateTimeProvider : IDateTimeProvider
    {
        private static readonly TimeSpan timeout;
        private static readonly TimeSpan timezone;
        private readonly RtcBase rtc;
        private TimeSpan lastUpdated;

        public DateTime Now
        {
            get;
            private set;
        }

        public RtcDateTimeProvider(RtcBase rtc)
        {
            this.rtc = rtc;
            lastUpdated = TimeSpan.Zero;
        }

        static RtcDateTimeProvider()
        {
            timeout = TimeSpan.FromSeconds(1L);
            timezone = TimeSpan.FromHours(3L);
        }

        public void Initialize()
        {
            Rtc.SetSystemTime(rtc.DateTime);
            Now = DateTime.UtcNow + timezone;
        }

        public void Update(TimeSpan elapsed)
        {
            lastUpdated += elapsed;

            if (timeout > lastUpdated)
            {
                return;
            }

            Now = DateTime.UtcNow + timezone;
            lastUpdated = TimeSpan.Zero;
        }
    }
}