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
    /// Tble to hold Time out periods (stale time) for queue times based on ItemType values.
    /// </summary>
    [Table(name: "ServerQueueTimeout")]
    public class DrpServerQueueTimeout
    {
        // default stale timeout period if none found in the table for the given ItemType
        public static readonly TimeSpan DefaultStaleTimeoutPeriod = TimeSpan.FromDays(1.0);

        [Key]
        [Required]
        public string ItemType { get; set; }

        [Required]
        public TimeSpan TimeoutPeriod { get; set; }
    }
}
