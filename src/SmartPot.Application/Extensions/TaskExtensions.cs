#nullable enable

using System.Threading.Tasks;

namespace SmartPot.Application.Extensions
{
    internal static class TaskExtensions
    {
        public static void FireAndForget(this Task? task)
        {
            if (null != task)
            {
                ;
            }
        }
    }
}

#nullable restore