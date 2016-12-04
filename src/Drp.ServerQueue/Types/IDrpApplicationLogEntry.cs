/*
    Copyright 2016 Peoplutions
*/

namespace Drp.Types
{
    #region Using Statements

    using System;

    #endregion

    /// <summary>
    /// Log Entry Interface
    /// </summary>
    public interface IDrpApplicationLogEntry
    {
        string LogEntry { get; set; }
        string LogId { get; set; }
        DrpApplicationLogLevel LogLevel {get; set; }
        Exception Exception { get; set; }
    }
}
