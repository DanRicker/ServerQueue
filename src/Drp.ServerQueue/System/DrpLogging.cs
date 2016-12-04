/*
    Copyright 2016 Peoplutions
*/

namespace Drp
{
    #region Using Statements

    using System;

    using Drp.Types;

    #endregion

    /// <summary>
    /// Internal logging
    /// </summary>
    public static class DrpLogging
    {
        // TODO: Get this from configuration
        public static DrpApplicationLogLevel ApplicationLogLevel = DrpApplicationLogLevel.Information;

        /// <summary>
        /// Internal logging
        /// </summary>
        /// <param name="drpLogEntry"></param>
        public static void WriteLogEntry(IDrpApplicationLogEntry drpLogEntry)
        {
            // Check logging level to determine if write is to be done
            if(false == drpLogEntry.LogLevel.WriteThisLogEntry(ApplicationLogLevel))
            {
                return;
            }

            // TODO: Other logging - Dependency Injection
            // For now, just concatinate and use Debug.WriteLine()

            string fullLogEntry = string.Empty;

            if (null != drpLogEntry)
            {
                if (null == drpLogEntry.Exception)
                {
                    fullLogEntry =
                        string.Format("[Id: {0}].[Entry: - {1}]",
                            drpLogEntry.LogId,
                            drpLogEntry.LogEntry);
                }

                else
                {
                    fullLogEntry =
                        string.Format("[Id: {0}].[Entry: - {1}]{2}Exception: {3}",
                            drpLogEntry.LogId,
                            drpLogEntry.LogEntry,
                            Environment.NewLine,
                            drpLogEntry.Exception.ToString());
                }
            }

            DrpDebugging.DebugWriteLine(fullLogEntry);
        }
    }
}
