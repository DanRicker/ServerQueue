/*
    Copyright 2016 Daniel Ricker III and Peoplutions
*/

namespace Drp.ServerQueueData.Models
{
    #region Using Statements

    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    #endregion

    /// <summary>
    /// Dequeued Items. Historical Data. Can be periodically archived. No Queue Operation use.
    /// </summary>
    [Table(name: "ServerQueueDequeuedItem")]
    public class DrpServerQueueDequeuedItem : DrpServerQueueStateEntry, IDrpServerQueueStateEntry
    {
        /// <summary>
        /// DO NOT USE
        /// Default empty constructor required by entity framework
        /// </summary>
        public DrpServerQueueDequeuedItem() { }

        /// <summary>
        /// Copy constructor : ALWAYS USE THIS CONSTRUCTOR
        /// </summary>
        /// <param name="serverQueueItem"></param>
        public DrpServerQueueDequeuedItem(DrpServerQueueAcquiredItem serverQueueItem)
        {
            this.Id = Guid.NewGuid();
            this.HistoryCreated = DateTimeOffset.UtcNow;
            this.ItemType = serverQueueItem.ItemType;
            this.QueueItemId = serverQueueItem.QueueItemId;
            this.Created = serverQueueItem.Created;
            this.Acquired = serverQueueItem.Acquired;
            this.AcquiredBy = serverQueueItem.AcquiredBy;
        }

        [Key]
        public Guid Id { get; set; }
        public DateTimeOffset Acquired { get; set; }
        public string AcquiredBy { get; set; }
        public DateTimeOffset HistoryCreated { get;  set; }

    }
}
