using Iot.Device.Ssd13xx;
using SmartPot.Core.Services;
using System;
using System.Diagnostics;

namespace SmartPot.Core.UI
{
    internal sealed class HomeScreen : IScreen
    {
        private static readonly TimeSpan Timeout;

        private readonly Ssd1306 display;
        private readonly IDateTimeProvider dateTimeProvider;
        private DateTime dateTime;
        private readonly char[] timeStr;

        public HomeScreen(Ssd1306 display, IDateTimeProvider dateTimeProvider)
        {
            this.display = display;
            this.dateTimeProvider = dateTimeProvider;
            timeStr = new char[5];
        }

        static HomeScreen()
        {
            Timeout = TimeSpan.FromSeconds(1L);
        }

        public void Initialize()
        {
            display.ClearScreen();
            display.Font = new BasicFont();
        }

        public bool ShouldDisplay(TimeSpan elapsed) => Timeout < elapsed;

        public void Display(TimeSpan _)
        {
            Debug.WriteLine($"Date: {dateTime}");

            WriteTimeBig();
            WriteDateSmall();

            display.Display();
        }

        public void Update(TimeSpan _)
        {
            dateTime = dateTimeProvider.Now;
        }

        private void PutNumber(int value, int offset)
        {
            timeStr[offset] = (char)('0' + (value / 10));
            timeStr[offset + 1] = (char)('0' + (value % 10));
        }

        private void WriteTimeBig()
        {
            PutNumber(dateTime.Hour, 0);
            timeStr[2] = 0 == (dateTime.Second & 0x01) ? ':' : ' ';
            PutNumber(dateTime.Minute, 3);

            var time = new String(timeStr);

            display.DrawString(12, 20, time, 2);
        }

        private void WriteDateSmall()
        {
            var date = $"{dateTime:d}";
            display.DrawString(24, 44, date);
        }
    }
}