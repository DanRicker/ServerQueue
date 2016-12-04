/*
    Copyright 2016 Peoplutions
*/

namespace Drp.SeverQueueData.Models
{
    #region Using Statements

    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    #endregion

    /// <summary>
    /// Enqueued Queue Items.
    /// </summary>
    [Table(name: "ServerQueueEnqueuedItem")]
    public class DrpServerQueueEnqueuedItem : DrpServerQueueStateEntry, IDrpServerQueueStateEntry
    {

        /// <summary>
        /// DO NOT USE
        /// Default empty constructor required by entity framework
        /// </summary>
        public DrpServerQueueEnqueuedItem() { }

        /// <summary>
        /// Create a new Enqueue Item based on an existing acquired item.
        /// Created is set to UtcNow to move Item to the bottom of the queue
        /// </summary>
        /// <param name="acquiredItem">The Acquired item that is to be requeued.</param>
        public DrpServerQueueEnqueuedItem(DrpServerQueueAcquiredItem acquiredItem)
        {
            this.Id = Guid.NewGuid();
            this.ItemType = acquiredItem.ItemType;
            this.QueueItemId = acquiredItem.QueueItemId;
            this.Created = DateTimeOffset.UtcNow;
        }

        [Key]
        public Guid Id { get; set; }
    }
}
