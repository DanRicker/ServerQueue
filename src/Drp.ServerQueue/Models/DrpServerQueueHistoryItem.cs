/*
    Copyright 2016 Daniel Ricker III and Peoplutions
*/

namespace Drp.ServerQueueData.Models
{
    #region Using Statements

    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using Drp.Types;

    #endregion

    /// <summary>
    /// The Database Table Object for History Items
    /// </summary>
    [Table(name:"ServerQueueHistoryItem")]
    public class DrpServerQueueHistoryItem : DrpServerQueueItemData, IDrpQueueItem
    {
        /// <summary>
        /// DO NOT USE
        /// Default empty constructor required by entity framework
        /// </summary>
        public DrpServerQueueHistoryItem() { }

        /// <summary>
        /// ALWAYS USE THIS CONSTRUCTOR
        /// Create a new server queue item
        /// </summary>
        /// <param name="itemType">User Defined String</param>
        /// <param name="itemId">User Defined String - Required - Set to Guid.Empty if null or whitespace </param>
        /// <param name="itemData">User Defined String</param>
        /// <param name="itemMetadata">User Defined String</param>
        public DrpServerQueueHistoryItem(DrpServerQueueItem serverQueueItem)
        {
            // ItemId is required. Does not have to be unique
            this.Id = serverQueueItem.Id;
            this.HistoryCreated = DateTimeOffset.UtcNow;
            this.ItemId = serverQueueItem.ItemId;
            this.ItemType = serverQueueItem.ItemType;
            this.ItemData = serverQueueItem.ItemData;
            this.ItemMetadata = serverQueueItem.ItemMetadata;
            this.Acquired = serverQueueItem.Acquired;
            this.AcquiredBy = serverQueueItem.AcquiredBy;
            this.Created = serverQueueItem.Created;
        }

        /// <summary>
        /// Queue Item Id
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Date and Time the Queue Item was created
        /// </summary>
        public DateTimeOffset HistoryCreated { get;  set; }


    }
}
