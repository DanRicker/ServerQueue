/*
    Copyright 2016 Peoplutions
*/

namespace Drp
{

    #region Using Statements

    using System;

    #endregion

    /// <summary>
    /// Server Status and Reporting
    /// </summary>
    public class ServerQueueState : ServerQueue, IServerQueue, IServerQueueState
    {
        // TODO: Add Server State information
        // Queue Items by ItemType, Counts (Enqueued and Acquired)
        // Queue Items Age by Item Type
        // Add Summaries from Queue Log (Transactional Reporting)
    }
}
