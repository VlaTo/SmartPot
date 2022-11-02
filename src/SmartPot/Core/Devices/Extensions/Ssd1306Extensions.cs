using Iot.Device.Ssd13xx;

namespace SmartPot.Core.Devices.Extensions
{
    internal static class Ssd1306Extensions
    {
        public static void DrawFilledRectangle2(this Ssd1306 oled, int x0, int y0,int width,int height)
        {
            //var bufferLength = oled.Pages * oled.Width + 4;
            //var buffer = oled.SliceGenericBuffer(bufferLength);
            oled.DrawFilledRectangle(x0, y0, width, height);
        }
    }
}