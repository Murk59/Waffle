﻿namespace Waffle.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Waffle.Retrying;

    /// <summary>
    /// Helpers for safely using Task libraries. 
    /// </summary>
    internal static class TaskHelpers
    {
        private static readonly Task DefaultCompleted = FromResult(default(VoidTaskResult));

        private static readonly Task<object> CompletedTaskReturningNull = FromResult<object>(null);

        /// <summary>
        /// Returns a canceled Task. The task is completed, IsCanceled = True, IsFaulted = False.
        /// </summary>
        internal static Task Canceled()
        {
            return CancelCache<VoidTaskResult>.Canceled;
        }

        /// <summary>
        /// Returns a canceled Task of the given type. The task is completed, IsCanceled = True, IsFaulted = False.
        /// </summary>
        internal static Task<TResult> Canceled<TResult>()
        {
            return CancelCache<TResult>.Canceled;
        }

        /// <summary>
        /// Returns a completed task that has no result. 
        /// </summary>        
        internal static Task Completed()
        {
            return DefaultCompleted;
        }

        /// <summary>
        /// Returns a completed task that has no result. 
        /// </summary>        
        internal static Task<TResult> Completed<TResult>()
        {
            return FromResult(default(TResult));
        }

        /// <summary>
        /// Returns an error task. The task is Completed, IsCanceled = False, IsFaulted = True.
        /// </summary>
        internal static Task FromError(Exception exception)
        {
            return FromError<VoidTaskResult>(exception);
        }

        /// <summary>
        /// Returns an error task of the given type. The task is Completed, IsCanceled = False, IsFaulted = True.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        internal static Task<TResult> FromError<TResult>(Exception exception)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        /// <summary>
        /// Returns an error task of the given type. The task is Completed, IsCanceled = False, IsFaulted = True.
        /// </summary>
        internal static Task FromErrors(IEnumerable<Exception> exceptions)
        {
            return FromErrors<VoidTaskResult>(exceptions);
        }

        /// <summary>
        /// Returns an error task of the given type. The task is Completed, IsCanceled = False, IsFaulted = True.
        /// </summary>
        internal static Task<TResult> FromErrors<TResult>(IEnumerable<Exception> exceptions)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetException(exceptions);
            return tcs.Task;
        }

        /// <summary>
        /// Returns a successful completed task with the given result.  
        /// </summary>        
        internal static Task<TResult> FromResult<TResult>(TResult result)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        internal static Task FromCancellation(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                throw new ArgumentOutOfRangeException("cancellationToken");
            }

            return new Task(null, true, cancellationToken, TaskCreationOptions.None);
        }

        internal static Task<object> NullResult()
        {
            return CompletedTaskReturningNull;
        }

        /// <summary>
        /// Return a task that runs all the tasks inside the iterator sequentially. It stops as soon
        /// as one of the tasks fails or cancels, or after all the tasks have run succesfully.
        /// </summary>
        /// <param name="asyncIterator">Collection of tasks to wait on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="disposeEnumerator">Whether or not to dispose the enumerator we get from <paramref name="asyncIterator"/>.
        /// Only set to <c>false</c> if you can guarantee that <paramref name="asyncIterator"/>'s enumerator does not have any resources it needs to dispose.</param>
        /// <returns>A task that signals completed when all the incoming tasks are finished.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is propagated in a Task.")]
        internal static Task Iterate(IEnumerable<Task> asyncIterator, CancellationToken cancellationToken = default(CancellationToken), bool disposeEnumerator = true)
        {
            Contract.Requires(asyncIterator != null);

            try
            {
                IEnumerator<Task> enumerator = asyncIterator.GetEnumerator();
                Task task = IterateImpl(enumerator, cancellationToken);
                return (disposeEnumerator && enumerator != null) ? task.Finally(enumerator.Dispose, runSynchronously: true) : task;
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        /// <summary>
        /// Provides the implementation of the Iterate method.
        /// Contains special logic to help speed up common cases.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is propagated in a Task.")]
        internal static Task IterateImpl(IEnumerator<Task> enumerator, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    // short-circuit: iteration canceled
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Canceled();
                    }

                    // short-circuit: iteration complete
                    if (!enumerator.MoveNext())
                    {
                        return Completed();
                    }

                    // fast case: Task completed synchronously & successfully
                    Task currentTask = enumerator.Current;
                    if (currentTask.Status == TaskStatus.RanToCompletion)
                    {
                        continue;
                    }

                    // fast case: Task completed synchronously & unsuccessfully
                    if (currentTask.IsCanceled || currentTask.IsFaulted)
                    {
                        return currentTask;
                    }

                    // slow case: Task isn't yet complete
                    return IterateImplIncompleteTask(enumerator, currentTask, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        /// <summary>
        /// Fallback for IterateImpl when the antecedent Task isn't yet complete.
        /// </summary>
        internal static Task IterateImplIncompleteTask(IEnumerator<Task> enumerator, Task currentTask, CancellationToken cancellationToken)
        {
            // There's a race condition here, the antecedent Task could complete between
            // the check in Iterate and the call to Then below. If this happens, we could
            // end up growing the stack indefinitely. But the chances of (a) even having
            // enough Tasks in the enumerator in the first place and of (b) *every* one
            // of them hitting this race condition are so extremely remote that it's not
            // worth worrying about.
            return currentTask.Then(() => IterateImpl(enumerator, cancellationToken));
        }

        /// <summary>
        /// Update the completion source if the task failed (cancelled or faulted). No change to completion source if the task succeeded. 
        /// </summary>
        /// <typeparam name="TResult">Result type of completion source.</typeparam>
        /// <param name="tcs">Completion source to update.</param>
        /// <param name="source">Task to update from.</param>
        /// <returns>True on success.</returns>
        internal static bool SetIfTaskFailed<TResult>(this TaskCompletionSource<TResult> tcs, Task source)
        {
            switch (source.Status)
            {
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    return tcs.TrySetFromTask(source);
            }

            return false;
        }

        /// <summary>
        /// Set a completion source from the given Task.
        /// </summary>
        /// <typeparam name="TResult">Result type for completion source.</typeparam>
        /// <param name="tcs">Completion source to set.</param>
        /// <param name="source">Task to get values from.</param>
        /// <returns>True if this successfully sets the completion source.</returns>
        [SuppressMessage("Microsoft.Web.FxCop", "MW1201:DoNotCallProblematicMethodsOnTask", Justification = "This is a known safe usage of Task.Result, since it only occurs when we know the task's state to be completed.")]
        internal static bool TrySetFromTask<TResult>(this TaskCompletionSource<TResult> tcs, Task source)
        {
            if (source.Status == TaskStatus.Canceled)
            {
                return tcs.TrySetCanceled();
            }

            if (source.Status == TaskStatus.Faulted)
            {
                return tcs.TrySetException(source.Exception.InnerExceptions);
            }

            if (source.Status == TaskStatus.RanToCompletion)
            {
                Task<TResult> taskOfResult = source as Task<TResult>;
                return tcs.TrySetResult(taskOfResult == null ? default(TResult) : taskOfResult.Result);
            }

            return false;
        }

        /// <summary>
        /// Set a completion source from the given Task. If the task ran to completion and the result type doesn't match
        /// the type of the completion source, then a default value will be used. This is useful for converting Task into
        /// Task{AsyncVoid}, but it can also accidentally be used to introduce data loss (by passing the wrong
        /// task type), so please execute this method with care.
        /// </summary>
        /// <typeparam name="TResult">Result type for completion source.</typeparam>
        /// <param name="tcs">Completion source to set.</param>
        /// <param name="source">Task to get values from.</param>
        /// <returns>True if this successfully sets the completion source.</returns>
        [SuppressMessage("Microsoft.Web.FxCop", "MW1201:DoNotCallProblematicMethodsOnTask", Justification = "This is a known safe usage of Task.Result, since it only occurs when we know the task's state to be completed.")]
        internal static bool TrySetFromTask<TResult>(this TaskCompletionSource<Task<TResult>> tcs, Task source)
        {
            if (source.Status == TaskStatus.Canceled)
            {
                return tcs.TrySetCanceled();
            }

            if (source.Status == TaskStatus.Faulted)
            {
                return tcs.TrySetException(source.Exception.InnerExceptions);
            }

            if (source.Status == TaskStatus.RanToCompletion)
            {
                // Sometimes the source task is Task<Task<TResult>>, and sometimes it's Task<TResult>.
                // The latter usually happens when we're in the middle of a sync-block postback where
                // the continuation is a function which returns Task<TResult> rather than just TResult,
                // but the originating task was itself just Task<TResult>. An example of this can be
                // found in TaskExtensions.CatchImpl().
                Task<Task<TResult>> taskOfTaskOfResult = source as Task<Task<TResult>>;
                if (taskOfTaskOfResult != null)
                {
                    return tcs.TrySetResult(taskOfTaskOfResult.Result);
                }

                Task<TResult> taskOfResult = source as Task<TResult>;
                if (taskOfResult != null)
                {
                    return tcs.TrySetResult(taskOfResult);
                }

                return tcs.TrySetResult(FromResult(default(TResult)));
            }

            return false;
        }

        /// <summary>
        /// Used as the T in a "conversion" of a Task into a Task{T}.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Size = 1)]
        internal struct VoidTaskResult
        {
        }

        /// <summary>
        /// This class is a convenient cache for per-type cancelled tasks.
        /// </summary>
        private static class CancelCache<TResult>
        {
            public static readonly Task<TResult> Canceled = GetCancelledTask();

            private static Task<TResult> GetCancelledTask()
            {
                TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
                tcs.SetCanceled();
                return tcs.Task;
            }
        }

        /// <summary>Starts a Task that will complete after the specified due time.</summary>
        /// <param name="dueTime">The delay in milliseconds before the returned task completes.</param>
        /// <returns>The timed Task.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="dueTime" /> argument must be non-negative or -1 and less than or equal to Int32.MaxValue.
        /// </exception>
        public static Task Delay(int dueTime)
        {
            return TaskHelpers.Delay(dueTime, CancellationToken.None);
        }

        /// <summary>Starts a Task that will complete after the specified due time.</summary>
        /// <param name="dueTime">The delay before the returned task completes.</param>
        /// <returns>The timed Task.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="dueTime" /> argument must be non-negative or -1 and less than or equal to Int32.MaxValue.
        /// </exception>
        public static Task Delay(TimeSpan dueTime)
        {
            return TaskHelpers.Delay(dueTime, CancellationToken.None);
        }

        /// <summary>Starts a Task that will complete after the specified due time.</summary>
        /// <param name="dueTime">The delay before the returned task completes.</param>
        /// <param name="cancellationToken">A CancellationToken that may be used to cancel the task before the due time occurs.</param>
        /// <returns>The timed Task.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="dueTime" /> argument must be non-negative or -1 and less than or equal to Int32.MaxValue.
        /// </exception>
        public static Task Delay(TimeSpan dueTime, CancellationToken cancellationToken)
        {
            long num = (long)dueTime.TotalMilliseconds;
            if (num < -1L || num > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("dueTime", "The timeout must be non-negative or -1, and it must be less than or equal to Int32.MaxValue.");
            }

            Contract.EndContractBlock();
            return TaskHelpers.Delay((int)num, cancellationToken);
        }

        /// <summary>Starts a Task that will complete after the specified due time.</summary>
        /// <param name="dueTime">The delay in milliseconds before the returned task completes.</param>
        /// <param name="cancellationToken">A CancellationToken that may be used to cancel the task before the due time occurs.</param>
        /// <returns>The timed Task.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="dueTime" /> argument must be non-negative or -1 and less than or equal to Int32.MaxValue.
        /// </exception>
        public static Task Delay(int dueTime, CancellationToken cancellationToken)
        {
            if (dueTime < -1)
            {
                throw new ArgumentOutOfRangeException("dueTime", "The timeout must be non-negative or -1, and it must be less than or equal to Int32.MaxValue.");
            }

            Contract.EndContractBlock();
            if (cancellationToken.IsCancellationRequested)
            {
                return new Task(() => { }, cancellationToken);
            }

            if (dueTime == 0)
            {
                return TaskHelpers.Completed();
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            CancellationTokenRegistration ctr = default(CancellationTokenRegistration);
            Timer timer = null;
            timer = new Timer(
                state =>
                {
                    ctr.Dispose();
                    timer.Dispose();
                    tcs.TrySetResult(true);
                    TimerManager.Remove(timer);
                },
            null,
            -1,
            -1);
            TimerManager.Add(timer);
            if (cancellationToken.CanBeCanceled)
            {
                ctr = cancellationToken.Register(() =>
                {
                    timer.Dispose();
                    tcs.TrySetCanceled();
                    TimerManager.Remove(timer);
                });
            }

            timer.Change(dueTime, -1);
            return tcs.Task;
        }
    }
}
