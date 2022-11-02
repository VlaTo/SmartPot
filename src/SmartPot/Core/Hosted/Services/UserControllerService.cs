using System;
using System.Threading;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Hosting;

namespace SmartPot.Core.Hosted.Services
{
    internal sealed class UserControllerService : BackgroundService
    {
        private readonly UserController controller;
        private IUserControllerState currentState;

        public IUserControllerState CurrentState
        {
            get => currentState;
            private set
            {
                if (ReferenceEquals(currentState, value))
                {
                    return;
                }

                if (null != currentState)
                {
                    currentState.Leave();
                }

                currentState = value;

                if (null != currentState)
                {
                    currentState.Enter();
                }
            }
        }

        public UserControllerService(UserController controller)
        {
            this.controller = controller;
        }

        // ReSharper disable once FunctionNeverReturns
        protected override void ExecuteAsync()
        {
            var wakeupCause = Sleep.GetWakeupCause();

            if (Sleep.WakeupCause.Ext0 == wakeupCause)
            {
                var gpioPin = Sleep.GetWakeupGpioPin();
                //currentState = new CheckSoilMoistureState();
            }


            /*var elapsed = TimeSpan.FromMilliseconds(Environment.TickCount64);
            var lastDisplayed = elapsed;
            var lastUpdated = elapsed;

            controller.Initialize();
            controller.Screen.Display(TimeSpan.Zero);
            
            while (true)
            {
                elapsed = TimeSpan.FromMilliseconds(Environment.TickCount64);

                controller.Update(elapsed - lastUpdated);

                lastUpdated = elapsed;
                elapsed = TimeSpan.FromMilliseconds(Environment.TickCount64);

                var duration = elapsed - lastDisplayed;
                var screen = controller.Screen;

                if (false == screen.ShouldDisplay(duration))
                {
                    Thread.SpinWait(100);
                    continue;
                }

                screen.Display(duration);
                lastDisplayed = elapsed;
            }*/
        }
    }
}