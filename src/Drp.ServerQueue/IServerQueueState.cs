namespace Drp
{

    #region Using Statements

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    #endregion

    public interface IServerQueueState
    {
        /// <summary>
        /// Count of Active items in the queue (Enqueued and Acquired)
        /// </summary>
        int QueueCount { get; }

        /// <summary>
        /// Count of Enqueued items
        /// </summary>
        int EnqueuedCount { get; }

        /// <summary>
        /// Count of Acquired items
        /// </summary>
        int AcquiredCount { get; }

        /// <summary>
        /// Count of active items (enqueued and acquired) in the queue of itemType
        /// </summary>
        /// <param name="itemType">Filter value for count</param>
        /// <returns>Count of items</returns>
        int QueueCountOfItemType(string itemType);

        /// <summary>
        /// Count of Enqueued items of itemType
        /// </summary>
        /// <param name="itemType">Filter value for count</param>
        /// <returns>Count of items</returns>
        int EnqueuedCountOfItemType(string itemType);

        /// <summary>
        /// Count of Acquired Items of itemType
        /// </summary>
        /// <param name="itemType">Filter value for count</param>
        /// <returns>Count of items</returns>
        int AcquiredCountOfItemType(string itemType);
    }
}
