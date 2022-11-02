
#nullable enable

using System.Threading;
using Android.OS;
using Java.Lang;
using Exception = System.Exception;
using Thread = Java.Lang.Thread;

namespace SmartPot.Application.Core
{
    internal sealed class WorkItemQueue
    {
        private WorkItemQueueThread? thread;

        public WorkItemQueue()
        {
        }

        public void Enqueue(IRunnable runnable, Bundle? data = null)
        {
            if (null == thread)
            {
                thread = new WorkItemQueueThread();
                thread.Start();
                thread.WaitHandle.WaitOne();
            }

            var message = Message.Obtain(thread.Handler, runnable);

            if (null == message)
            {
                return;
            }

            if (null != data)
            {
                message.Data = data;
            }

            message.SendToTarget();
        }

        #region WorkItemQueue execution thread

        private sealed class WorkItemQueueThread : Thread
        {
            private readonly ManualResetEvent mre;
            private Looper? looper;

            public Handler? Handler
            {
                get;
                private set;
            }

            public EventWaitHandle WaitHandle => mre;

            public WorkItemQueueThread()
            {
                looper = null;
                mre = new ManualResetEvent(false);
            }

            public override void Run()
            {
                Looper.Prepare();

                looper = Looper.MyLooper();

                if (null == looper)
                {
                    throw new Exception();
                }

                Handler = new Handler(looper);

                mre.Set();

                Looper.Loop();
            }
        }
        
        #endregion
    }
}

#nullable restore