using System;

namespace SmartPot.Core.UI
{
    internal interface IScreen : IUpdateable
    {
        void Initialize();

        bool ShouldDisplay(TimeSpan elapsed);

        void Display(TimeSpan elapsed);
    }
}
