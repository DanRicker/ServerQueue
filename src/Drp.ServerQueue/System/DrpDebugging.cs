/*
    Copyright 2016 Peoplutions
*/
namespace Drp
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Debugging utilities
    /// </summary>
    public static class DrpDebugging
    {

        public static readonly string DefaultLogDateTimeStampFormat = "yyyyMMddHHmmss.fff";
        public static readonly string DefaultUIDateTimeStampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        public static readonly string DefaultLogSizeElementFormat = "#.000";
        public static readonly string DefaultLogSizeFormat = "W: {0}, H: {1}";
        public static readonly string DefaultApplicationName = "Drp.Application";


        public static bool HadFatalException { get; set; }

#if DEBUG
        private static bool _doDebugWrite = true;
#else
        private static bool _doDebugWrite = false;
#endif

        /// <summary>
        /// Application Name property
        /// </summary>
        /// <returns>application name string</returns>
        private static string GetApplicationName()
        {
            // for now, return default
            return DrpDebugging.DefaultApplicationName;
        }

        /// <summary>
        /// Application Description property
        /// </summary>
        /// <returns>applcation description string</returns>
        private static string GetApplicationDescription()
        {
            return "Drp.Application.Description";
        }

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
        /// Dump the current state of the application to the debug console
        /// </summary>
        public static string DebugWriteState()
        {
            List<string> ret = new List<string>();
            if (_doDebugWrite)
            {
                try
                {
                    string installPath = DrpDebugging.GetInstallFullPath();

                    string textLine = string.Format("==== Application State {0} ====", DateTimeOffset.UtcNow.ToString(DefaultUIDateTimeStampFormat));
                    ret.Add(textLine);
                    Debug.WriteLine(textLine);

                    textLine = string.Format(" -- InstallationFolder: {0}", installPath);
                    ret.Add(textLine);
                    Debug.WriteLine(textLine);

                    // Add whatever other details desired here.

                }
                catch (System.Exception ex)
                {
                    // Don't want to call:
                    //     DrpExceptionHandler.HandleException("DrpDebugging.DebugWriteState", ex);
                    // here. Will Create a circular reference
                    string errMsg = ex.Message;
                    Debug.WriteLine("Exception executing DrpDebugging.DebugWriteState() : {0}", ex.ToString());
                }

            }

            // return the debug written text
            return string.Join(Environment.NewLine, ret);

        }

        /// <summary>
        /// Determines whether Debug Console is written to or not
        /// </summary>
        public static bool DoDebugWrite
        {
            get { return _doDebugWrite; }
            set { _doDebugWrite = value; }
        }

        /// <summary>
        /// Write a line to the debug console
        /// </summary>
        /// <param name="textLine">Line to write</param>
        public static void DebugWriteLine(string textLine)
        {
            try
            {
                Debug.WriteLine(textLine);
            }
            catch (System.Exception ex)
            {
                // Don't want to call:
                //     DrpExceptionHandler.HandleException("DrpDebugging.DebugWriteLine", ex);
                // here. The Call will create a circular reference
                string errMsg = ex.Message;
#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
#endif
                // Don't call Debug.WriteLine() again. It just through an exception;
            }
        }

        /// <summary>
        /// Write a line to the debu console
        /// </summary>
        /// <param name="prefix">Prefix to prepend to written line</param>
        /// <param name="textLine">Line to write</param>
        public static void DebugWriteLine(string prefix, string textLine)
        {
            try
            {
                DrpDebugging.DebugWriteLine(string.Format("{0} - {1}", prefix, textLine));
            }
            catch (System.Exception ex)
            {
                // Don't want to call:
                //     DrpExceptionHandler.HandleException("DrpDebugging.DebugWriteLine", ex);
                // here. The Call will create a circular reference
                string errMsg = ex.Message;
                Debug.WriteLine("Exception executing DrpDebugging.DebugWriteLine(prefix textLine) : {0}", ex.ToString());
            }
        }

        /// <summary>
        /// print line formatting
        /// </summary>
        /// <param name="prefix">prefix for line</param>
        /// <param name="value">value to print</param>
        /// <returns></returns>
        private static string makeDebugLine(string prefix, float value)
        {
            return string.Format("{0}: {1}", prefix, value.ToString("#.000"));
        }

        /// <summary>
        /// print line formatting
        /// </summary>
        /// <param name="prefix">prefix for line</param>
        /// <param name="value">value to print</param>
        /// <returns></returns>
        private static string makeDebugLine(string prefix, string value)
        {
            return string.Format("{0}: {1}", prefix, value);
        }

    }
}
