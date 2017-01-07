/*
    Copyright 2016 Daniel Ricker III and Peoplutions
*/

namespace Drp.ServerQueueData.Models
{

    #region Using Statements

    using System;

    #endregion

    /// <summary>
    /// Base interface for Enqueued, Acquired, and Dequeued Queue State objects
    /// </summary>
    internal interface IDrpServerQueueStateEntry
    {
        /// <summary>
        /// Queue Item Data Id
        /// </summary>
        Guid QueueItemId { get; set; }

        /// <summary>
        /// Queue Item ItemType (category)
        /// </summary>
        string ItemType { get; set; }

        /// <summary>
        /// Time Stamp when Queue Item was created
        /// </summary>
        DateTimeOffset Created { get; set; }
    }
}
