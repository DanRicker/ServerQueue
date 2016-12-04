/*
    Copyright 2016 Peoplutions
*/

namespace Drp
{
    #region Using Statements

    using Drp.SeverQueueData.DAL;

    #endregion

    /// <summary>
    /// Internal ServerQueueContext. (Database Context)
    /// Encapsulates DrpServerQueueDataContext creation and connection string state.
    /// </summary>
    internal class ServerQueueDataContext
    {

        /// <summary>
        /// ServerQueueDataContext instance.DO NOT USE DIRECTLY
        /// - Call GetDbContext() which ensures the context is connected.
        /// </summary>
        private DrpServerQueueDataContext drpServerQueueDataContext = null;

        /// <summary>
        /// Get the current connection string.
        /// If no connection string has been set, then get the default from settings
        /// </summary>
        /// <returns>Connection string for the queue database</returns>
        private string GetConnnectionString()
        {
            string ret = this.ConnectionString;
            if (string.IsNullOrWhiteSpace(ret))
            {
                ret = Properties.Settings.Default.DefaultConnectionString;
            }
            return ret;
        }

        /// <summary>
        /// Get the current instance of the database context
        /// </summary>
        /// <returns>DrpServerQueueDataContext</returns>
        internal DrpServerQueueDataContext GetDbContext()
        {
            if (null == drpServerQueueDataContext)
            {
                drpServerQueueDataContext = DrpServerQueueDataContext.NewDrpServerQueueDataContext(this.GetConnnectionString());
            }
            return drpServerQueueDataContext;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">optional connection string</param>
        internal ServerQueueDataContext(string connectionString = null)
        {
            drpServerQueueDataContext = DrpServerQueueDataContext.NewDrpServerQueueDataContext(connectionString);
            // Persist the full connection string from the database. it could be a default string used
            this.ConnectionString = drpServerQueueDataContext.Database.Connection.ConnectionString;
        }

        /// <summary>
        /// Saved Connection String 
        /// </summary>
        internal string ConnectionString
        {
            get; private set;
        }

    }
}
