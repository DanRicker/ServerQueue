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
    /// The purpose of this class is to break up the object model hierarchy so that the
    /// Entity Framework will see the Active and History objects as different.
    ///     Active is ItemData and History is ItemData, but neither Active nor History IS the other
    /// When the object heirarchy was such that History in heritied from Active, then
    ///     -- Local [Active DbSet] in memory held Active and History items
    ///     This mixing of object types was not desired.
    ///     
    ///     Now, though there is some duplication of some definitions between Active and History items,
    ///     the Entity Frame work object context no longer mixes the object types.
    ///     -- Active DbSet holds only Active objects
    /// </summary>
    public class DrpServerQueueItemData
    {

        public const int QueueItemTypeMaxSize = 255;
        public const int QueueItemIdMaxSize = 255;
        public const int AquiredByMaxSize = 255;

        /// <summary>
        /// Ensure string values are not greater than max length. Truncate if required.
        /// </summary>
        /// <param name="value">string to check</param>
        /// <param name="maxLength">max length value</param>
        /// <returns>String less than or equal to max length</returns>
        public static string EnsureMaxLength(string value, int maxLength)
        {
            if (false == string.IsNullOrWhiteSpace(value) && value.Length > maxLength)
            {
                value = value.Substring(0, maxLength);
            }
            return value;
        }

        /// <summary>
        /// User Defined category/grouping value
        /// </summary>
        [MaxLength(QueueItemTypeMaxSize)]
        [Index("idxItemType", IsClustered = false, IsUnique = false)]
        public string ItemType { get; set; }

        /// <summary>
        /// Date and Time the Queue Item was created
        /// </summary>
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// User Defined Id for the queue item data.
        /// </summary>
        [Required]
        [MaxLength(QueueItemIdMaxSize)]
        [Index("idxItemId", IsClustered = false, IsUnique = false)]
        public string ItemId { get; set; }

        /// <summary>
        /// The data held in the queue item (JSON, XML, etc)
        /// </summary>
        [Required]
        public string ItemData { get; set; }

        /// <summary>
        /// User defined metadata of the data in this queue item (JSON, XML, etc)
        /// </summary>
        public string ItemMetadata { get; set; }

        /// <summary>
        /// Who/What acquired (took ownership of) this queue item
        /// </summary>
        [MaxLength(AquiredByMaxSize)]
        public string AcquiredBy { get; set; }

        /// <summary>
        /// Date and Time that this queue item was acquired
        /// </summary>
        public DateTimeOffset? Acquired { get; set; }

        /// <summary>
        /// Acquire Convenience method.
        ///     Sets AcquiredBy to acquiredBy argument and sets Acquired to UtcNow
        ///  NOTE: both Acquired and Acquired should be read only but that doesn't work
        ///  with entity framework.
        /// </summary>
        /// <param name="acquiredBy">Who/What is acquiring this queue item</param>
        internal virtual void Acquire(string acquiredBy)
        {
            this.AcquiredBy = acquiredBy;
            this.Acquired = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void Release()
        {
            this.AcquiredBy = null;
            this.Acquired = null;
        }

    }
}
