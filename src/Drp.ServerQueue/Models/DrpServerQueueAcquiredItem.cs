﻿/*
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
    /// This class is here because EntityFrameWork doesn't handle using the same
    ///     class for two different DbSet objects. It silently through an error that I
    ///     found by trying to read properties on the db context object.
    /// 
    /// The only indication of this failure was that the entity framework initialization code did not run
    ///     and the DbSet objects were null
    /// </summary>
    [Table(name: "ServerQueueAcquiredItem")]
    public class DrpServerQueueAcquiredItem : DrpServerQueueStateEntry, IDrpServerQueueStateEntry
    {


        /// <summary>
        /// Default empty constructor required by entity framework
        /// </summary>
        public DrpServerQueueAcquiredItem() { }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="serverQueueItem"></param>
        public DrpServerQueueAcquiredItem(DrpServerQueueEnqueuedItem serverQueueItem)
        {
            this.Id = Guid.NewGuid();
            this.Created = serverQueueItem.Created;
            this.QueueItemId = serverQueueItem.QueueItemId;
            this.ItemType = serverQueueItem.ItemType;
        }

        [Key]
        public Guid Id { get; set; }
        public DateTimeOffset Acquired { get; set; }
        public string AcquiredBy { get; set; }

    }
}
