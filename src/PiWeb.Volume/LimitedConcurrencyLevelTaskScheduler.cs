#region Copyright

// Source: https://docs.microsoft.com/de-de/dotnet/api/system.threading.tasks.taskscheduler?view=netframework-4.8
// Source: https://devblogs.microsoft.com/pfxteam/parallelextensionsextras-tour-7-additional-taskschedulers/
//--------------------------------------------------------------------------
//
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//
//  File: LimitedConcurrencyTaskScheduler.cs
//
//--------------------------------------------------------------------------

#endregion

namespace Zeiss.IMT.PiWeb.Volume
{
	#region usings

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	#endregion

	/// <summary>
	/// Provides a task scheduler that ensures a maximum concurrency level while
	/// running on top of the ThreadPool.
	/// </summary>
	internal class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
	{
		#region members

		[ThreadStatic] private static bool _CurrentThreadIsProcessingItems;

		private readonly LinkedList<Task> _Tasks = new LinkedList<Task>();
		private int _DelegatesQueuedOrRunning;

		#endregion

		#region constructors

		/// <summary>
		/// Initializes an instance of the LimitedConcurrencyLevelTaskScheduler class with the
		/// specified degree of parallelism.
		/// </summary>
		/// <param name="maxDegreeOfParallelism">The maximum degree of parallelism provided by this scheduler.</param>
		public LimitedConcurrencyLevelTaskScheduler( int maxDegreeOfParallelism )
		{
			if( maxDegreeOfParallelism < 1 ) throw new ArgumentOutOfRangeException( nameof(maxDegreeOfParallelism) );
			MaximumConcurrencyLevel = maxDegreeOfParallelism;
		}

		#endregion

		#region properties

		/// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
		public sealed override int MaximumConcurrencyLevel { get; }

		#endregion

		#region methods

		/// <summary>Queues a task to the scheduler.</summary>
		/// <param name="task">The task to be queued.</param>
		protected sealed override void QueueTask( Task task )
		{
			// Add the task to the list of tasks to be processed.  If there aren't enough
			// delegates currently queued or running to process tasks, schedule another.
			lock( _Tasks )
			{
				_Tasks.AddLast( task );
				if( _DelegatesQueuedOrRunning < MaximumConcurrencyLevel )
				{
					++_DelegatesQueuedOrRunning;
					NotifyThreadPoolOfPendingWork();
				}
			}
		}

		/// <summary>
		/// Informs the ThreadPool that there's work to be executed for this scheduler.
		/// </summary>
		private void NotifyThreadPoolOfPendingWork()
		{
			ThreadPool.UnsafeQueueUserWorkItem( _ =>
			{
				// Note that the current thread is now processing work items.
				// This is necessary to enable inlining of tasks into this thread.
				_CurrentThreadIsProcessingItems = true;
				try
				{
					// Process all available items in the queue.
					while( true )
					{
						Task item;
						lock( _Tasks )
						{
							// When there are no more items to be processed,
							// note that we're done processing, and get out.
							if( _Tasks.Count == 0 )
							{
								--_DelegatesQueuedOrRunning;
								break;
							}

							// Get the next item from the queue
							item = _Tasks.First.Value;
							_Tasks.RemoveFirst();
						}

						// Execute the task we pulled out of the queue
						TryExecuteTask( item );
					}
				}
				// We're done processing items on the current thread
				finally
				{
					_CurrentThreadIsProcessingItems = false;
				}
			}, null );
		}

		/// <summary>Attempts to execute the specified task on the current thread.</summary>
		/// <param name="task">The task to be executed.</param>
		/// <param name="taskWasPreviouslyQueued"></param>
		/// <returns>Whether the task could be executed on the current thread.</returns>
		protected sealed override bool TryExecuteTaskInline( Task task, bool taskWasPreviouslyQueued )
		{
			// If this thread isn't already processing a task, we don't support inlining
			if( !_CurrentThreadIsProcessingItems ) return false;

			// If the task was previously queued, remove it from the queue
			if( taskWasPreviouslyQueued ) TryDequeue( task );

			// Try to run the task.
			return TryExecuteTask( task );
		}

		/// <summary>Attempts to remove a previously scheduled task from the scheduler.</summary>
		/// <param name="task">The task to be removed.</param>
		/// <returns>Whether the task could be found and removed.</returns>
		protected sealed override bool TryDequeue( Task task )
		{
			lock( _Tasks ) return _Tasks.Remove( task );
		}

		/// <summary>Gets an enumerable of the tasks currently scheduled on this scheduler.</summary>
		/// <returns>An enumerable of the tasks currently scheduled.</returns>
		protected sealed override IEnumerable<Task> GetScheduledTasks()
		{
			bool lockTaken = false;
			try
			{
				Monitor.TryEnter( _Tasks, ref lockTaken );
				if( lockTaken ) return _Tasks.ToArray();
				else throw new NotSupportedException();
			}
			finally
			{
				if( lockTaken ) Monitor.Exit( _Tasks );
			}
		}

		#endregion
	}
}