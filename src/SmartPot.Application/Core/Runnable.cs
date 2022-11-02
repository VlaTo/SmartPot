
#nullable enable

using System;

namespace SmartPot.Application.Core
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class Runnable : Java.Lang.Object, Java.Lang.IRunnable
    {
        private readonly Action action;

        public Runnable(Action action)
        {
            this.action = action;
        }

        public void Run() => action.Invoke();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class Runnable<T> : Java.Lang.Object, Java.Lang.IRunnable
    {
        private readonly T arg0;
        private readonly Action<T> action;

        public Runnable(Action<T> action, T arg0)
        {
            this.arg0 = arg0;
            this.action = action;
        }

        public void Run() => action.Invoke(arg0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    internal sealed class Runnable<T0, T1> : Java.Lang.Object, Java.Lang.IRunnable
    {
        private readonly T0 arg0;
        private readonly T1 arg1;
        private readonly Action<T0, T1> action;

        public Runnable(Action<T0, T1> action, T0 arg0, T1 arg1)
        {
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.action = action;
        }

        public void Run() => action.Invoke(arg0, arg1);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    internal sealed class Runnable<T0, T1, T2> : Java.Lang.Object, Java.Lang.IRunnable
    {
        private readonly T0 arg0;
        private readonly T1 arg1;
        private readonly T2 arg2;
        private readonly Action<T0, T1, T2> action;

        public Runnable(Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
        {
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.action = action;
        }

        public void Run() => action.Invoke(arg0, arg1, arg2);
    }
}

#nullable restore