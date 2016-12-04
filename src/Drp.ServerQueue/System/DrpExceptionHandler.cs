/*
    Copyright 2016 Peoplutions
*/
namespace Drp
{
    #region Using Statements

    using System;
    using System.Diagnostics;

    #endregion

    /// <summary>
    /// Core Exception Handling and logging
    /// </summary>
    public static class DrpExceptionHandler
    {

        /// <summary>
        /// Exception ID for possible thread tracking/tracing
        /// </summary>
        /// <returns>New Guid unformatted string</returns>
        public static string NewExceptionInstanceId()
        {
            return Guid.NewGuid().ToStringNoFormatting();
        }

        /// <summary>
        /// Log exception information
        /// </summary>
        /// <param name="source">string source of the entry</param>
        /// <param name="sourceException">source exception</param>
        /// <param name="exceptionInstanceId">Optional - id value for multi-thread tracing</param>
        public static void LogException(string source, Exception sourceException, string exceptionInstanceId = null)
        {
            string logText = sourceException.ToString();

            if (false == string.IsNullOrWhiteSpace(exceptionInstanceId))
            {
                logText = string.Format("{0} - {1}", exceptionInstanceId, logText);
            }

            try
            {
                DrpDebugging.DebugWriteLine(source, logText);
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (System.OutOfMemoryException memEx)
#pragma warning restore CS0168 // Variable is declared but never used
            {
                // DO NOTHING... just bail out
#if DEBUG
                // TODO: Add debug handling code
#endif

                return;
            }
            catch(System.Exception ex)
            {
                // All other exceptions just Debug.WriteLine directly to warning of bigger issue...
                string errMsg = ex.Message;
                Debug.WriteLine("Exception executing DrpDebugging.DebugWriteLine(prefix textLine) : {0}", logText);
            }
        }

        public static void HandleException(string source, Exception ex)
        {
            DrpExceptionHandler.LogException(source, ex);

#if DEBUG
            if(System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
#endif
            // TODO: Bubble up hidden exceptions for diagnostics
        }

    }
}
