/*
    Copyright 2016 Daniel Ricker III and Peoplutions
*/

namespace Drp.Types
{
    #region Using Statements

    using System;

    #endregion

    /// <summary>
    /// Base Queue Item data contract
    /// </summary>
    public interface IDrpQueueItem
    {
        Guid Id { get;  }
        string ItemId { get; }
        string ItemType { get; }
        DateTimeOffset Created { get;  }
        string AcquiredBy { get; }
        DateTimeOffset? Acquired { get; }
        string ItemData { get; }
        string ItemMetadata { get; }
    }
}
