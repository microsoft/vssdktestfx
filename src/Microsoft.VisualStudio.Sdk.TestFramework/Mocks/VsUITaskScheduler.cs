// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Sdk.TestFramework
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Threading;

    /// <summary>
    /// A <see cref="TaskScheduler"/> that executes tasks on the mocked up UI thread.
    /// </summary>
    internal class VsUITaskScheduler : TaskScheduler
    {
        private readonly JoinableTaskFactory jtf;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsUITaskScheduler"/> class.
        /// </summary>
        /// <param name="jtf">The <see cref="JoinableTaskFactory"/> to use to switch to the UI thread.</param>
        internal VsUITaskScheduler(JoinableTaskFactory jtf)
        {
            Requires.NotNull(jtf, nameof(jtf));
            this.jtf = jtf;
        }

        /// <inheritdoc />
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override void QueueTask(Task task)
        {
            using (this.jtf.Context.SuppressRelevance())
            {
                this.jtf.RunAsync(async delegate
                {
                    await this.jtf.SwitchToMainThreadAsync(alwaysYield: true);
                    this.TryExecuteTask(task);
                }).Task.Forget();
            }
        }

        /// <inheritdoc />
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            throw new NotImplementedException();
        }
    }
}
