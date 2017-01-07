/*
    Copyright 2016 Daniel Ricker III and Peoplutions
*/


namespace Drp
{

    #region Using Statements

    using System;
    using Drp.Types;

    #endregion

    /// <summary>
    /// Contract for ServerQueue. Entity Framework Database Transactions are
    ///     used to ensure a queue item is only acquired and dequeued once.
    ///     
    /// 
    /// For the ServerQueue the intended work flow is:
    ///    1- WorkFlowA-Output -> Enqueues(workItem)
    ///    2- WorkFlowB-Input -> Aquires(workItem) - Thissets the AcquireId value needed to Release or Dequeue
    ///    3- WorkFlowB does work.
    ///    4- If Work Succeeded Then WorkFlowB -> Dequeues(workItem)
    ///    5- If Work Failed Then WorkFlowB -> Releases(workItem) OR Attempts to do work again (loop back to 3)
    /// 
    /// The queue is agnostic to the queue item contents.
    /// </summary>
    public interface IServerQueue
    {
        /// <summary>
        /// Enqueue an item. An item consists of 4 string properties. The string values for data and metadata can be JSON, xml,
        /// other data converted to a string.
        /// </summary>
        /// <param name="type">User defined grouping of queue items</param>
        /// <param name="dataId">User defined Id value such as an original object id value, or workflow Id value</param>
        /// <param name="data">User defined data as a string (serialization/deserialization)</param>
        /// <param name="metadata">User defined metadata as a string</param>
        /// <returns>IDrpQueueItem object</returns>
        IDrpQueueItem Enqueue(string type, string dataId, string data, string metadata);

        /// <summary>
        /// Retrieve the next item to be dequeued
        /// </summary>
        /// <returns>Next Queue Item if any exist</returns>
        IDrpQueueItem Peak();

        /// <summary>
        /// Retrieved the Next Item to be Dequeued where type = argument value
        /// </summary>
        /// <param name="type">Type to find</param>
        /// <returns>First Queue item with Type value</returns>
        IDrpQueueItem Peak(string type);

        /// <summary>
        /// Retrieves the specific queue item where queue item id equals the argument value.
        /// If the specified Queue Item has been acquired it will not be found.
        /// </summary>
        /// <param name="queueItemId">Queue Item Id to find</param>
        /// <returns>Specified Queue Item if found</returns>
        IDrpQueueItem Peak(Guid queueItemId);

        /// <summary>
        /// Retrieves a queue item with ItemId (user defined id) value specified
        /// </summary>
        /// <param name="itemId">User Item Id to find</param>
        /// <returns>First Queue item found with the given ItemId value</returns>
        IDrpQueueItem PeakItemId(string itemId);

        /// <summary>
        /// Marks a queue item as acquired. An acquired item can only be dequeued by the same acquirerId value.
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="queueItemId">Id of queue item to release</param>
        /// <returns>Acquired Item on success, null on failure</returns>
        IDrpQueueItem Acquire(string acquirerId, Guid queueItemId);

        /// <summary>
        /// Marks a queue item as acquired. An acquired item can only be dequeued by the same acquirerId value.
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="itemType">The item type (category) to acquire</param>
        /// <returns>Acquired Item on success, null on failure</returns>
        IDrpQueueItem Acquire(string acquirerId, string itemType);

        /// <summary>
        /// Releases a previously acquired queue item
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="queueItemId">Id of queue item to release</param>
        /// <returns>Released Item on success, null on failure</returns>
        IDrpQueueItem Release(string acquirerId, Guid queueItemId);

        /// <summary>
        /// Dequeues an Item. The Item must have been previously Acquired.
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="queueItemId">Id of queue item to release</param>
        /// <returns>Dequeued Item on success, null on failure</returns>
        IDrpQueueItem Dequeue(string acquirerId, Guid queueItemId);


    }
}