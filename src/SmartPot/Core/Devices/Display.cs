using System.Device.I2c;
using Iot.Device.Ssd13xx;

namespace SmartPot.Core.Devices
{
    internal sealed class Display : Ssd1306
    {
        public Display(I2cDevice i2cDevice)
            : base(i2cDevice)
        {
        }

        public Display(I2cDevice i2cDevice, DisplayResolution res)
            : base(i2cDevice, res)
        {
        }
    }
}