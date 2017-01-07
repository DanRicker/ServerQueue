/*
    Copyright 2016 Daniel Ricker III and Peoplutions
*/

namespace Drp
{
    #region Using Statements

    using Drp.ServerQueueData.DAL;

    #endregion

    /// <summary>
    /// Internal ServerQueueContext. (Database Context)
    /// Encapsulates DrpServerQueueDataContext creation and connection string state.
    /// </summary>
    internal class ServerQueueDataContext
    {

        /// <summary>
        /// Retrieve the max "GetDbContext calls before creating new context.
        /// -- Comprimise between creating every time and contantly increasing memory due to object context
        /// </summary>
        /// <returns></returns>
        private int GetDbContectCountMax()
        {
            // TODO: Make configurable
            return 1000;
        }

        private int getDbContextCount = 0;

        /// <summary>
        /// ServerQueueDataContext instance.DO NOT USE DIRECTLY
        /// - Call GetDbContext() which ensures the context is connected.
        /// </summary>
        private DrpServerQueueDataContext drpServerQueueDataContext = null;

        /// <summary>
        /// Get the current instance of the database context
        /// </summary>
        /// <returns>DrpServerQueueDataContext</returns>
        internal DrpServerQueueDataContext GetDbContext()
        {
            getDbContextCount++;
            // TODO: find a better measure for disposing of the context
            if (getDbContextCount > GetDbContectCountMax())
            {
                DrpServerQueueDataContext tmp = this.drpServerQueueDataContext;
                this.drpServerQueueDataContext = null;
                tmp.Dispose();
                getDbContextCount = 0;
                DrpDebugging.DebugWriteLine("ServerQueueDataContext.GetDbContext() - recreating");
            }

            if (null == drpServerQueueDataContext)
            {
                drpServerQueueDataContext = DrpServerQueueDataContext.NewDrpServerQueueDataContext(this.ConnectionString);
            }
            return drpServerQueueDataContext;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">optional connection string</param>
        internal ServerQueueDataContext(string connectionString)
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
