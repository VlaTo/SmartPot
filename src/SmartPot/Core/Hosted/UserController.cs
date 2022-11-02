using System;
using Iot.Device.Ssd13xx;
using SmartPot.Core.Services;
using SmartPot.Core.UI;

namespace SmartPot.Core.Hosted
{
    internal sealed class UserController : IUpdateable
    {
        private readonly Ssd1306 oled;
        private readonly IDateTimeProvider dateTimeProvider;

        public IScreen Screen
        {
            get;
            private set;
        }

        public UserController(Ssd1306 oled, IDateTimeProvider dateTimeProvider)
        {
            this.oled = oled;
            this.dateTimeProvider = dateTimeProvider;
        }

        public void Initialize()
        {
            Screen = new HomeScreen(oled, dateTimeProvider);
            dateTimeProvider.Initialize();
            Screen.Initialize();
        }

        public void Update(TimeSpan elapsed)
        {
            dateTimeProvider.Update(elapsed);
            Screen.Update(elapsed);
        }
    }
}