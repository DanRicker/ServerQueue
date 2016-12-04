using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drp
{
    /// <summary>
    /// Server Queue Administrative Methods and Properties
    /// </summary>
    public interface IServerQueueAdministation : IServerQueue
    {

        /// <summary>
        /// Returns ServerQueueItems that were acquired longer than staleTimePeriod and have the
        ///     specified itemType value.
        ///     if staleTimePeriod is less than TimeSpan.Zero,
        ///         all ServerQueueItems of the specified itemType value are returned.
        ///     if true == string.IsNullOrWhiteSpace(itemType) then item type is removed from the filter.
        /// </summary>
        /// <param name="itemType">ItemType value to include. If null or whitespace then include all</param>
        /// <param name="staleTimePeriod">Time period subracted from UctNow for aquired value filtering</param>
        /// <returns>All Queue Items that are stale based on the argument values</returns>
        IList<Guid> GetStaleServerQueueAcquiredItems(string itemType, TimeSpan staleTimePeriod);

        /// <summary>
        /// Queue Maintenance action. Items can be acquired but forgotten (external system errors)
        /// 
        /// Requeue Acquired Items meeting the itemType and staleTimePeriod criteria
        /// Add an acquired item back into the queue (un-Acquire)
        /// This
        ///     - removes acquired and acquired by values
        ///     - keeps the Queue Item Id and Created values
        ///     - increments that StaleAcquireCount value
        /// </summary>
        /// <param name="itemType">ItemType value to include. If null or whitespace then include all</param>
        /// <param name="staleTimePeriod">Time period subracted from UctNow for aquired value filtering</param>
        /// <returns>Tuple of int values countSuccess and countAll. If countSuccess == countAll then all 
        ///             the items meeting the criteria were requeued
        /// </returns>
        Tuple<int, int> RequeueStaleAcquiredItems(string itemType, TimeSpan staleTimePeriod);

        /// <summary>
        /// Returns a list of Queue Item Id Values for items that are stale and of itemType
        ///    Stale = Uct.Now.Subtrack(staleTimePeriod)
        /// </summary>
        /// <param name="itemType">Item Time to filter by. If blank, all item types returned</param>
        /// <param name="staleTimePeriod">Time period for which anything older than is stale.</param>
        /// <returns></returns>
        IList<Guid> GetStaleServerQueueEnqueuedItems(string itemType, TimeSpan staleTimePeriod);

    }
}
