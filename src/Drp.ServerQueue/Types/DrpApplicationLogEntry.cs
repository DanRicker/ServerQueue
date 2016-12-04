/*
    Copyright 2016 Peoplutions
*/

namespace Drp.Types
{
    #region Using Statements

    using System;

    #endregion 
    
    /// <summary>
    /// Log Entry Class
    /// </summary>
    public class DrpApplicationLogEntry : IDrpApplicationLogEntry
    {
        public string LogId { get; set; }
        public DrpApplicationLogLevel LogLevel { get; set; }
        public string LogEntry { get; set; }
        public Exception Exception { get; set; }
    }
}
