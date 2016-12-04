/*
    Copyright 2016 Peoplutions
*/

namespace Drp
{
    #region Using Statements

    using System;

    using Drp.Types;
    using Drp.SeverQueueData.DAL;

    #endregion

    /// <summary>
    /// Public facing ServerQueue. Does not directly expose the queue data model.
    /// Exposes Queue Methods.
    /// 
    /// For the ServerQueue the intended work flow is:
    ///    -- WorkFlowA-Output -> Enqueues(workItem)
    ///    -- WorkFlowB-Input -> Aquires(workItem)
    ///    -- WorkFlowB does work.
    ///    -- WorkFlowB -> Dequeues(workItem)
    /// 
    /// The queue is agnostic to the queue item contents.
    /// </summary>
    public class ServerQueue : IServerQueue
    {

        private ServerQueueDataContext serverQueueContext = null;

        /// <summary>
        /// ServerQueueDataContext instance.DO NOT USE DIRECTLY
        /// - Call GetDbContext() which ensures the context is connected.
        /// </summary>
        //private DrpServerQueueDataContext serverQueueDataContext = null;

        /// <summary>
        /// Get the current connection string.
        /// If no connection string has been set, then get the default from settings
        /// </summary>
        /// <returns>Connection string for the queue database</returns>
        //private string GetConnnectionString()
        //{
        //    string ret = this.ConnectionString;
        //    if (string.IsNullOrWhiteSpace(ret))
        //    {
        //        ret = Properties.Settings.Default.DefaultConnectionString;
        //    }
        //    return ret;
        //}

        /// <summary>
        /// Get the current instance of the database context
        /// </summary>
        /// <returns>DrpServerQueueDataContext</returns>
        internal DrpServerQueueDataContext GetDbContext()
        {
            return serverQueueContext.GetDbContext();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">optional connection string</param>
        public ServerQueue(string connectionString = null)
        {
            serverQueueContext = new ServerQueueDataContext(connectionString);
        }

        /// <summary>
        /// Count of Active items in the queue (Enqueued and Acquired)
        /// </summary>
        public int QueueCount {  get { return this.GetDbContext().QueueCount; } }

        /// <summary>
        /// Count of Enqueued items
        /// </summary>
        public int EnqueuedCount {  get { return this.GetDbContext().EnqueuedCount; } }

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

        /// <summary>
        /// Enqueue an item. An item consists of 4 string properties. The string values for data and metadata can be JSON, xml,
        /// other data converted to a string.
        /// </summary>
        /// <param name="type">User defined grouping of queue items</param>
        /// <param name="dataId">User defined Id value such as an original object id value, or workflow Id value</param>
        /// <param name="data">User defined data as a string (serialization/deserialization)</param>
        /// <param name="metadata">User defined metadata as a string</param>
        /// <returns>IDrpQueueItem object</returns>
        public IDrpQueueItem Enqueue(string type, string dataId, string data, string metadata)
        {
            return this.GetDbContext().Enqueue(type, dataId, data, metadata);
        }

        /// <summary>
        /// Retrieve the next item to be dequeued
        /// </summary>
        /// <returns>Next Queue Item if any exist</returns>
        public IDrpQueueItem Peak()
        {
            return this.GetDbContext().Peak();
        }

        /// <summary>
        /// Retrieved the Next Item to be Dequeued where type = argument value
        /// </summary>
        /// <param name="itemType">Type to find</param>
        /// <returns>First Queue item with Type value</returns>
        public IDrpQueueItem Peak(string itemType)
        {
            return this.GetDbContext().Peak(itemType);
        }

        /// <summary>
        /// Retrieves the specific queue item where queue item id equals the argument value.
        /// If the specified Queue Item has been acquired it will not be found.
        /// </summary>
        /// <param name="queueItemId">Queue Item Id to find</param>
        /// <returns>Specified Queue Item if found</returns>
        public IDrpQueueItem Peak(Guid queueItemId)
        {
            return this.GetDbContext().Peak(queueItemId);
        }

        /// <summary>
        /// Retrieves a queue item with ItemId (user defined id) value specified
        /// </summary>
        /// <param name="itemId">User Item Id to find</param>
        /// <returns>First Queue item found with the given ItemId value</returns>
        public IDrpQueueItem PeakItemId(string itemId)
        {
            return this.GetDbContext().PeakItemId(itemId);
        }

        /// <summary>
        /// Marks a queue item as acquired. An acquired item can only be dequeued by the same acquirerId value.
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="queueItemId">Id of queue item to release</param>
        /// <returns>Acquired Item on success, null on failure</returns>
        public IDrpQueueItem Acquire(string acquirerId, Guid queueItemId)
        {
            return this.GetDbContext().Acquire(acquirerId, queueItemId);
        }

        /// <summary>
        /// Marks a queue item as acquired. An acquired item can only be dequeued by the same acquirerId value.
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="itemType">Queue Item Type (category) to acquire</param>
        /// <returns>Acquired Item on success, null on failure</returns>
        public IDrpQueueItem Acquire(string acquirerId, string itemType)
        {
            return this.GetDbContext().Acquire(acquirerId, itemType);
        }

        /// <summary>
        /// Releases a previously acquired queue item
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="queueItemId">Id of queue item to release</param>
        /// <returns>Released Item on success, null on failure</returns>
        public IDrpQueueItem Release(string acquirerId, Guid queueItemId)
        {
            return this.GetDbContext().Release(acquirerId, queueItemId);
        }

        /// <summary>
        /// Dequeues an Item. The Item must have been previously Acquired.
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="queueItemId">Id of queue item to release</param>
        /// <returns>Dequeued Item on success, null on failure</returns>
        public IDrpQueueItem Dequeue(string acquirerId, Guid queueItemId)
        {
            return this.GetDbContext().Dequeue(acquirerId, queueItemId);
        }


    }
}
