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
    /// The Database Table Object for Active Items
    /// </summary>
    [Table(name:"ServerQueueItem")]
    public class DrpServerQueueItem : DrpServerQueueItemData, IDrpQueueItem
    {

        /// <summary>
        /// DO NOT USE
        /// Default empty constructor required by entity framework
        /// </summary>
        public DrpServerQueueItem() { }

        /// <summary>
        /// Create a new server queue item
        /// </summary>
        /// <param name="itemType">User Defined String</param>
        /// <param name="itemId">User Defined String - Required - Set to Guid.Empty if null or whitespace </param>
        /// <param name="itemData">User Defined String</param>
        /// <param name="itemMetadata">User Defined String</param>
        public DrpServerQueueItem(string itemType, string itemId, string itemData, string itemMetadata)
        {
            // ItemId is required. Does not have to be unique
            this.ItemId = string.IsNullOrWhiteSpace(itemId) ? Guid.Empty.ToStringOuterParenthesis() : DrpServerQueueItemData.EnsureMaxLength(itemId, DrpServerQueueItemData.QueueItemIdMaxSize);
            this.ItemType = DrpServerQueueItemData.EnsureMaxLength(itemType, DrpServerQueueItemData.QueueItemTypeMaxSize);
            this.ItemData = itemData;
            this.ItemMetadata = itemMetadata;
            this.Created = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Queue Item Id
        /// </summary>
        [Key]
        public Guid Id { get;  set; }

    }
}
