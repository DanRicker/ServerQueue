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
    /// Queue Action (Enqueue, Acquire, Release, Dequeue) Log Entry.
    /// </summary>
    [Table(name: "ServerQueueLog")]
    public class DrpServerQueueLogEntry
    {
        /// <summary>
        /// Table Id for the log entry
        /// </summary>
        [Key]
        public Guid LogEntryId { get; set; }

        /// <summary>
        /// Source Queue State Entry Id. Guid.Empty for Enqueue action
        /// </summary>
        [Required]
        public Guid SourceQueueEntryId { get; set; }

        /// <summary>
        /// Destination Queue State Entry.
        /// </summary>
        [Required]
        public Guid DestinationQueueEntryId { get; set; }

        /// <summary>
        /// Queue Item (User Data Object) Id
        /// </summary>
        [Required]
        public Guid QueueItemId { get; set; }

        /// <summary>
        /// Log Entry Time stamp
        /// </summary>
        [Required]
        public DateTimeOffset LogDateTime { get; set; }

        /// <summary>
        /// Log Category. Internally defined for queue actions 
        /// </summary>
        public string LogCategory { get; set; }

        /// <summary>
        /// Log Entry Details
        /// </summary>
        [Required]
        public string LogEntry { get; set; }
    }
}
