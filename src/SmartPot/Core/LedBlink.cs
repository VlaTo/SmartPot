using System.Device.Gpio;
using System.Threading;

namespace SmartPot.Core
{
    internal sealed class LedBlink
    {
        private readonly GpioPin pin;
        private readonly CancellationToken cancellationToken;

        public LedBlink(GpioPin pin, CancellationToken cancellationToken)
        {
            this.pin = pin;
            this.cancellationToken = cancellationToken;
        }

        public void Run()
        {
            while (false == cancellationToken.IsCancellationRequested)
            {
                pin.Toggle();

                if (cancellationToken.WaitHandle.WaitOne(500, true))
                {
                    break;
                }
            }

            pin.Write(PinValue.Low);
        }
    }
}