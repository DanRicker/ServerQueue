/*
    Copyright 2016 Daniel Ricker III and Peoplutions
*/
namespace Drp
{

    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Debugging utilities
    /// </summary>
    public static class DrpDebugging
    {

        private static string _drpProcessInstanceId = string.Format("DRP-{0}", Guid.NewGuid().ToStringNoFormatting());
        
        private static TraceListener _traceListener = null;

        public static string DrpProcessInstanceId
        {
            get { return _drpProcessInstanceId; }
        }

        public static void SetTraceListener(TraceListener traceListener)
        {
            _traceListener = traceListener;
        }

        public static TraceListener TraceListener
        {
            get
            {
                if (null == _traceListener)
                {
                    string basePath = Path.GetDirectoryName(GetInstallFullPath());
                    string fileRelativePath = string.Format(@"DrpProcessLogs\{0}.log", _drpProcessInstanceId);
                    string filePath = Path.Combine(basePath, fileRelativePath);
                    string fileDirectory = Path.GetDirectoryName(filePath);
                    if (false == Directory.Exists(fileDirectory))
                    {
                        Directory.CreateDirectory(fileDirectory);
                    }
                    _traceListener = new TextWriterTraceListener(filePath, _drpProcessInstanceId);
                }
                return _traceListener;
            }

            set
            {
                _traceListener = value;
            }
        }


        public static readonly string DefaultLogDateTimeStampFormat = "yyyyMMddHHmmss.fff";
        public static readonly string DefaultUIDateTimeStampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        public static readonly string DefaultLogSizeElementFormat = "#.000";
        public static readonly string DefaultLogSizeFormat = "W: {0}, H: {1}";
        public static readonly string DefaultApplicationName = "Drp.Application";

        // TODO: Deal with fatal exceptions - restart process??
        public static bool HadFatalException { get; set; }

        /// <summary>
        /// Assembly Full Path property
        /// </summary>
        /// <returns>application full path string</returns>
        private static string GetInstallFullPath()
        {
            string ret = string.Empty;
            try
            {
                Assembly thisAssembly = Assembly.GetExecutingAssembly();

                if (null != thisAssembly)
                {
                    ret = thisAssembly.Location;
                }
            }
            catch (System.Exception ex)
            {
                ret = string.Format("Error Getting 'Package.Current.InstalledLocation.Path': {0}", ex.Message);
                Drp.DrpExceptionHandler.LogException("DrpDebugging.GetInstallFullPath()", ex);
            }
            return ret;
        }

        /// <summary>
        /// Write a line to the debug console
        /// </summary>
        /// <param name="textLine">Line to write</param>
        public static void DebugWriteLine(string textLine)
        {
            try
            {
                DrpDebugging.TraceListener.WriteLine(string.Format("{0} || {1}", DateTimeOffset.UtcNow.ToString(DrpDebugging.DefaultLogDateTimeStampFormat) ,textLine));
                DrpDebugging.TraceListener.Flush();
            }
            catch (System.Exception ex)
            {
                // Don't want to call:
                //     DrpExceptionHandler.HandleException("DrpDebugging.DebugWriteLine", ex);
                // here. The Call will create a circular reference
                string errMsg = ex.Message;
                DrpDebugging.HadFatalException = true;
#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    //System.Diagnostics.Debugger.Break();
                }
#endif
            }
        }

    }
}
