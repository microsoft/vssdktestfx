using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    internal class VsUITaskScheduler : TaskScheduler
    {
        private readonly JoinableTaskFactory jtf;

        internal VsUITaskScheduler(JoinableTaskFactory jtf)
        {
            Requires.NotNull(jtf, nameof(jtf));
            this.jtf = jtf;
        }

        protected override IEnumerable<System.Threading.Tasks.Task> GetScheduledTasks()
        {
            throw new NotImplementedException();
        }

        protected override void QueueTask(System.Threading.Tasks.Task task)
        {
            using (this.jtf.Context.SuppressRelevance())
            {
                this.jtf.RunAsync(async delegate
                {
                    await this.jtf.SwitchToMainThreadAsync();
                    this.TryExecuteTask(task);
                });
            }
        }

        protected override bool TryExecuteTaskInline(System.Threading.Tasks.Task task, bool taskWasPreviouslyQueued)
        {
            throw new NotImplementedException();
        }
    }

}
