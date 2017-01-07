/*
    Copyright 2016 Daniel Ricker III and Peoplutions
*/

namespace Drp
{

    #region Using Statements

    using System;

    #endregion

    /// <summary>
    /// Server Status and Reporting
    /// </summary>
    public class ServerQueueState : ServerQueue, IServerQueue, IServerQueueState
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        public ServerQueueState(string connectionString = null)
            : base(connectionString)
        { }

        // TODO: Add Server State information
        // Queue Items by ItemType, Counts (Enqueued and Acquired)
        // Queue Items Age by Item Type
        // Add Summaries from Queue Log (Transactional Reporting)
        /// <summary>
        /// Count of Active items in the queue (Enqueued and Acquired)
        /// </summary>
        public int QueueCount { get { return this.GetDbContext().QueueCount; } }

        /// <summary>
        /// Count of Enqueued items
        /// </summary>
        public int EnqueuedCount { get { return this.GetDbContext().EnqueuedCount; } }

        /// <summary>
        /// Count of Acquired items
        /// </summary>
        public int AcquiredCount { get { return this.GetDbContext().AcquiredCount; } }

        /// <summary>
        /// Count of active items (enqueued and acquired) in the queue of itemType
        /// </summary>
        /// <param name="itemType">Filter value for count</param>
        /// <returns>Count of items</returns>
        public int QueueCountOfItemType(string itemType)
        {
            return this.GetDbContext().QueueCountOfItemType(itemType);
        }

        /// <summary>
        /// Count of Enqueued items of itemType
        /// </summary>
        /// <param name="itemType">Filter value for count</param>
        /// <returns>Count of items</returns>
        public int EnqueuedCountOfItemType(string itemType)
        {
            return this.GetDbContext().EnqueuedCountOfItemType(itemType);
        }

        /// <summary>
        /// Count of Acquired Items of itemType
        /// </summary>
        /// <param name="itemType">Filter value for count</param>
        /// <returns>Count of items</returns>
        public int AcquiredCountOfItemType(string itemType)
        {
            return this.GetDbContext().AcquiredCountOfItemType(itemType);
        }
    }
}
