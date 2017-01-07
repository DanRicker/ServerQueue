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
    /// Entity Framework will see the Enqueued, Acquired, and Dequeued objects as different.
    /// When the object heirarchy was such that Dequeued in heritied from Acquired which in turn
    /// inheritied from Enqueued, then
    ///     -- Enqueued DbSet in memory held Enqueued, Acquired and Dequeued items
    ///     -- Acquired DbSet in memory held Acquired and Dequeued items
    ///     This mixing of object types was not desired.
    ///     
    ///     Now, though there is some duplication of some definitions between Enqueued, Acquired, and Dequeued,
    ///     the Entity Frame work object context no longer mixes the object types.
    ///     -- Enqueued DbSet holds only Enqueued objects
    ///     -- Acquired DbSet hold only Acquired objects
    /// </summary>
    public class DrpServerQueueStateEntry : IDrpServerQueueStateEntry
    {
        /// <summary>
        /// Id if QueueData object 
        /// </summary>
        [Index("idxQueueItemId", IsUnique = true, IsClustered = false)]
        public Guid QueueItemId { get; set; }

        /// <summary>
        /// User Defined Type to categorize queue items
        /// </summary>
        [MaxLength(DrpServerQueueItem.QueueItemTypeMaxSize)]
        [Index("idxItemType", IsUnique = false, IsClustered = false)]
        public string ItemType { get; set; }

        /// <summary>
        /// DateTime that this queue entry was created.
        /// </summary>
        public DateTimeOffset Created { get; set; }
    }
}
