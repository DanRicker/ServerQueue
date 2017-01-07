/*
    Copyright 2016 Daniel Ricker III and Peoplutions

    This file contains both the enumeration definition and extension methods for the defined enumeration

*/

namespace Drp.Types
{
    #region Using Statements

    using System;

    #endregion

    /// <summary>
    /// Application Logging Level
    /// </summary>
    public enum DrpApplicationLogLevel
    {
        Information = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }

    /// <summary>
    /// Extension Methods for DrpAppLogLevel
    /// </summary>
    public static class DrpApplicationLogLevelExtension
    {

        /// <summary>
        /// Should this DrpAppLogLevel value be written to the log
        /// </summary>
        /// <param name="thisEnumValue">This enumeration value</param>
        /// <param name="settingEnumValue">The setting value for writting logs to be compared</param>
        /// <returns>true if this enum value is greater than or equal to the settingEnumValue</returns>
        public static bool WriteThisLogEntry(this DrpApplicationLogLevel thisEnumValue, DrpApplicationLogLevel settingEnumValue)
        {
            return thisEnumValue >= settingEnumValue;
        }
    }

}
