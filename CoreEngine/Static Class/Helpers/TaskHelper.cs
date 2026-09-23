using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEngine.Helpers
{
    public static class TaskHelper
    {
        public static async Task WaitUntil(Func<bool> predicate, 
            CancellationToken cancellationToken = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            while (!predicate())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }
    }
}
