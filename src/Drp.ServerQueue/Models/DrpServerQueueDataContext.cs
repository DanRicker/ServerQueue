/*
    Copyright 2016 Daniel Ricker III and Peoplutions
*/

namespace Drp.ServerQueueData.DAL
{

    #region Using Statements

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration.Conventions;

    using Drp;
    using Drp.ServerQueueData.Models;
    using Drp.Types;
    using System.Data.SqlClient;

    #endregion

    /// <summary>
    /// Server Queue Data Context. Controls the contents of the three queue tables using queue like methods.
    ///     To help ensure that an item is dequeued this context does not use "tracking"
    ///         so the local DB contect is ignored for queued operations
    ///     For reporting, the local object context is used. Reporting does not need to deal with the issues
    ///         involved with ensuring a single consumer.
    /// </summary>
    internal class DrpServerQueueDataContext : DbContext, IServerQueue
    {

        #region Static Methods and Fields

        /// <summary>
        /// Returns array of SQL SQL Statements to be run as part of database creation.
        /// </summary>
        /// <returns>IEnumerable of string</returns>
        private static IEnumerable<string> GetDatabaseBuildSqlExecutables()
        {
            return new string[]
            {
                // Currently Just Create Procedure Statements but could easily be others.
                Drp.Properties.DrpServerQueue.spQueueItemAcquire,
                Drp.Properties.DrpServerQueue.spQueueItemAcquireSpecific,
                Drp.Properties.DrpServerQueue.spQueueItemEnqueue,
                Drp.Properties.DrpServerQueue.spQueueItemRelease,
                Drp.Properties.DrpServerQueue.spQueueItemDequeue,
            };
        }

        /// <summary>
        /// Number of times to try to acquire an item before returning failure
        /// </summary>
        /// <returns></returns>
        public static int GetAcquireRetryAttempts()
        {
            // TODO: Make dynamic and add config setting
            return 10;
        }

        /// <summary>
        /// See: http://stackoverflow.com/questions/22741476/how-to-consume-an-entity-framewok-v6-library
        /// This here to ensure all the code first related DLLs are included in build outputs of 
        ///     any consumers of this DLL.
        ///     Without this static method to instantiate an instance object, the build tools don't
        ///     get any dependency references that point to the code first DLLs
        /// </summary>
        static DrpServerQueueDataContext()
        {
            var ensureDllIsCopied = System.Data.Entity.SqlServer.SqlProviderServices.Instance;
        }

        /// <summary>
        /// Initializes the DrpServerQueueDataContext.
        /// </summary>
        /// <param name="connString">Optional. If null or blank then will use internal default 
        ///         connection string to local sqlExpress server.</param>
        /// <returns></returns>
        internal static DrpServerQueueDataContext NewDrpServerQueueDataContext(string connString)
        {
            DrpServerQueueDataContext ret = new DrpServerQueueDataContext(connString);
            return ret;
        }

        #endregion

        #region Database Tables

        /// <summary>
        /// The Queue Item Tables (Active and History)
        /// </summary>
        public DbSet<DrpServerQueueItem> ServerQueueItems { get; set; }
        public DbSet<DrpServerQueueHistoryItem> ServerQueueHistoryItems { get; set; }


        /// <summary>
        /// Three Tables to hold the queue items in the 3 states.
        ///     - ServerQueueItems: Items added to the queue
        ///     - ServerQueueAquiredItems: Items that have been acquired
        ///     - ServerQueueDequeuedItems: History table of queue items that have passed through this queue
        /// </summary>
        public DbSet<DrpServerQueueEnqueuedItem> ServerQueueEnqueuedItems { get; set; }
        public DbSet<DrpServerQueueAcquiredItem> ServerQueueAcquiredItems { get; set; }
        public DbSet<DrpServerQueueDequeuedItem> ServerQueueDequeuedItems { get; set; }

        /// <summary>
        /// Each Queue Action is recorded in the Server Queue Logs table
        /// </summary>
        public DbSet<DrpServerQueueLogEntry> ServerQueueLogs { get; set; }

        /// <summary>
        /// TODO: Build out the timeout processing
        /// </summary>
        public DbSet<DrpServerQueueTimeout> ServerQueueTimeout { get; set; }

        #endregion

        #region Private/Protected methods

        /// <summary>
        /// Write a Server Queue Log Entry to the database.
        ///  - Wrapped in a transaction.
        /// </summary>
        /// <param name="logEntry">Log Entry to write</param>
        private void WriteQueueLogEntry(DrpServerQueueLogEntry logEntry)
        {
            using (DbContextTransaction transaction = this.Database.BeginTransaction())
            {
                try
                {
                    this.ServerQueueLogs.Add(logEntry);
                    this.SaveChanges();
                    transaction.Commit();
                }
                catch(System.Exception ex)
                {
                    // Don't want logging failures to crash the service.
                    transaction.Rollback();
                    this.WriteAppLogEntry(
                        "DrpServerQueueDataContext.WriteQueueLogEntry",
                        "Exception Writing to Queue Log in Database",
                        ex);
                    Drp.DrpExceptionHandler.LogException(
                        "DrpServerQueueDataContext.WriteLogQueueAction RolledBack",
                        ex);
                }
            }
        }

        /// <summary>
        /// Convenience method to writing queue log entires
        /// </summary>
        /// <param name="queueItemId">queueItemId of queue entity</param>
        /// <param name="category">Log Category</param>
        /// <param name="logEntry">Log Entry</param>
        private void WriteQueueLogEntry(Guid sourceQueueEntryId, Guid destinationQueueEntryId, Guid queueItemId, string category, string logEntry)
        {
            this.WriteQueueLogEntry(new DrpServerQueueLogEntry()
            {
                LogEntryId = Guid.NewGuid(),
                SourceQueueEntryId = sourceQueueEntryId,
                DestinationQueueEntryId = destinationQueueEntryId,
                QueueItemId = queueItemId,
                LogDateTime = DateTimeOffset.UtcNow,
                LogCategory = category,
                LogEntry = logEntry
            });
        }

        /// <summary>
        /// Internal Application Logging. This logging is about tracking the code, not the queue items
        /// This logging is not intended to be integrated with the QueueItem Tracking in the ServerQueueLog table.
        /// </summary>
        /// <param name="logId">Log Id - typically string object heirarchy. Defaults to
        ///         "DrpServerQueueDataContext.WriteAppLogEntry" if null or blank</param>
        /// <param name="logEntry">Log entry to write. Required</param>
        /// <param name="ex">exception to be included with the log entry</param>
        private void WriteAppLogEntry(string logId, string logEntry, Exception ex = null)
        {
            if(string.IsNullOrWhiteSpace(logEntry))
            {
                throw new ArgumentNullException("logEntry");
            }
            if (string.IsNullOrWhiteSpace(logId))
            {
                logId = "DrpServerQueueDataContext.WriteAppLogEntry";
            }

            // Write the log
            Drp.DrpLogging.WriteLogEntry(new DrpApplicationLogEntry()
            {
                LogId = logId,
                LogEntry = logEntry,
                LogLevel = DrpApplicationLogLevel.Information,
                Exception = ex

            });
        }

        /// <summary>
        /// Override OnModelCreating to complete mapping
        ///  - Inherited Properties need to be explicitly mapped
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            base.OnModelCreating(modelBuilder);
        }


        /// <summary>
        /// Create the Server Queue Stored Procedures.
        /// Load "Create" text from included SQL Command file. Executes the Create Proc against the database.
        /// </summary>
        private void CreateStoredProcedures()
        {
            if (null == this.Database)
            {
                throw new InvalidOperationException("Create Stored Procedure Failed. Database is null");
            }

            foreach(string createSprocText in GetDatabaseBuildSqlExecutables())
            {
                this.Database.ExecuteSqlCommand(createSprocText);
            }
        }


        #endregion

        #region Constructor/Destructor

        /// <summary>
        /// Internal constructor only
        /// </summary>
        /// <param name="connectionString"></param>
        internal DrpServerQueueDataContext(string connectionString)
            : base(connectionString)
        {
            // Initialize Configuration
            this.Configuration.AutoDetectChangesEnabled = true;

            // The stored procedures for Queue Operations use Transactions.
            // Don't need transactions around transactions here and nothing else should need it.
            this.Configuration.EnsureTransactionsForFunctionsAndCommands = true;

            this.Configuration.LazyLoadingEnabled = true;
            this.Configuration.ProxyCreationEnabled = true;
            this.Configuration.ValidateOnSaveEnabled = true;

            // Static SetInitializer for DbContext Database
            Database.SetInitializer<DrpServerQueueDataContext>(new CreateDatabaseIfNotExists<DrpServerQueueDataContext>());
            if (this.Database.CreateIfNotExists())
            {
                // On Create, need to also create the stored procedures.
                this.CreateStoredProcedures();
            }

            // Queue operations are done in a transaction.
            // Use "force == true"
            // from: https://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k(System.Data.Entity.Database.Initialize);k(TargetFrameworkMoniker-.NETFramework,Version%3Dv4.6);k(DevLang-csharp)&rd=true
            //      This method is typically used when it is necessary to ensure that the
            //      database has been created and seeded before starting some operation
            //      where doing so lazily will cause issues, such as when the operation
            //      is part of a transaction.
            this.Database.Initialize(true);

        }


        #endregion


        #region IServerQueue interface methods

        /// <summary>
        /// Count of active items (enqueued and acquired) in the queue
        ///     Count of active queue items (not historical)
        /// </summary>
        public int QueueCount
        {
            get
            {
                if (null != ServerQueueItems)
                {
                    return ServerQueueItems.Count();
                }
                return 0;
            }
        }

        /// <summary>
        /// Count of items Enqueued.
        /// </summary>
        public int EnqueuedCount
        {
            get
            {
                if (null != ServerQueueEnqueuedItems)
                {
                    return ServerQueueEnqueuedItems.AsNoTracking().Count();
                }
                return 0;
            }
        }


        /// <summary>
        /// Count of items Acquired
        /// </summary>
        public int AcquiredCount
        {
            get
            {
                if (null != ServerQueueAcquiredItems)
                {
                    return ServerQueueAcquiredItems.AsNoTracking().Count();
                }
                return 0;
            }
        }

        /// <summary>
        /// Count of active items (enqueued and acquired) in the queue of itemType
        /// </summary>
        /// <param name="itemType">Filter value for count</param>
        /// <returns>Count of items</returns>
        public int QueueCountOfItemType(string itemType)
        {
            if (string.IsNullOrWhiteSpace(itemType))
            {
                return this.QueueCount;
            }
            else if (null != this.ServerQueueItems)
            {
                return this.ServerQueueItems.Where(sqi => sqi.ItemType.Equals(itemType, StringComparison.Ordinal)).Count();
            }
            return 0;
        }

        /// <summary>
        /// Count of Enqueued items of itemType
        /// </summary>
        /// <param name="itemType">Filter value for count</param>
        /// <returns>Count of items</returns>
        public int EnqueuedCountOfItemType(string itemType)
        {
            if (string.IsNullOrWhiteSpace(itemType))
            {
                return this.EnqueuedCount;
            }
            else if (null != this.ServerQueueEnqueuedItems)
            {
                return this.ServerQueueEnqueuedItems.AsNoTracking().Where(sqi => sqi.ItemType.Equals(itemType, StringComparison.Ordinal)).Count();
            }
            return 0;
        }

        /// <summary>
        /// Count of Acquired Items of itemType
        /// </summary>
        /// <param name="itemType">Filter value for count</param>
        /// <returns>Count of items</returns>
        public int AcquiredCountOfItemType(string itemType)
        {
            if (string.IsNullOrWhiteSpace(itemType))
            {
                return this.EnqueuedCount;
            }
            else if (null != this.ServerQueueAcquiredItems)
            {
                return this.ServerQueueAcquiredItems.AsNoTracking().Where(sqi => sqi.ItemType.Equals(itemType, StringComparison.Ordinal)).Count();
            }
            return 0;
        }

        /// <summary>
        /// Enqueue an item. An item consists of 4 string properties. The string values for data and metadata can be JSON, xml,
        /// other data converted to a string.
        /// Adding the Queue Item is done using a database transaction. This allows a rollback on failure.
        /// </summary>
        /// <param name="type">User defined grouping of queue items</param>
        /// <param name="dataId">User defined Id value such as an original object id value, or workflow Id value</param>
        /// <param name="data">User defined data as a string (serialization/deserialization)</param>
        /// <param name="metadata">User defined metadata as a string</param>
        /// <returns>IDrpQueueItem object</returns>
        public IDrpQueueItem Enqueue(string type, string dataId, string data, string metadata)
        {
            DrpServerQueueItem ret = null;
            Guid queueStateId = Guid.Empty;

            /*

            CREATE PROCEDURE [dbo].[spQueueItemEnqueue]
                @itemId NVARCHAR(255) = NULL,
                @itemType NVARCHAR(255) = NULL,
                @itemData NVARCHAR(MAX),
                @itemMetadata NVARCHAR(MAX)

            */

            using (SqlCommand sqlCommand = new SqlCommand(
                "exec dbo.spQueueItemEnqueue @itemId, @itemType, @itemData, @itemMetadata"))
            {

                sqlCommand.Connection = (SqlConnection)this.Database.Connection;
                if (sqlCommand.Connection.State != System.Data.ConnectionState.Open)
                {
                    sqlCommand.Connection.Open();
                }

                sqlCommand.Parameters.AddWithValue("@itemId", dataId);
                sqlCommand.Parameters.AddWithValue("@itemType", type);
                sqlCommand.Parameters.AddWithValue("@itemData", data);
                sqlCommand.Parameters.AddWithValue("@itemMetadata", metadata);

                try
                {
                    // If nothing returned, then Enqueue failed
                    using (SqlDataReader sqlreader = sqlCommand.ExecuteReader())
                    {
                        while(sqlreader.Read())
                        {
                            queueStateId = (Guid)sqlreader["Id"];
                            // Don't make a DB Call for the actual QueueItem object.
                            // The only missing information is the Id (QueueItemId) and that was just retrieved
                            ret = new DrpServerQueueItem()
                            {
                                Id = (Guid)sqlreader["QueueItemId"],
                                ItemType = type,
                                ItemData = data,
                                ItemId = dataId,
                                ItemMetadata = metadata
                            };
                            // There should only be one but just in case... exit here
                            break;
                        }

                        if (sqlreader.Read())
                        {
                            this.WriteAppLogEntry(
                                "[Enqueue].[Issue]",
                                string.Format("sqlReader return more than one row: [@itemId: {0},  @itemType: {1}]", dataId, type));
                        }
                    }

                }
                catch (System.Exception ex)
                {
                    this.WriteAppLogEntry(
                        "[Enqueue].[Failure]",
                        string.Format("[@itemId: {0},  @itemType: {1}]", dataId, type),
                        ex);
                    ret = null;
                }
            }

            if (null != ret)
            {
                this.WriteQueueLogEntry(
                    Guid.Empty,
                    queueStateId,
                    ret.Id,
                    "[Enqueue].[Success]",
                    string.Format("[Enqueue Id: {0} Item [type: {1}, dataId: {2}]", ret.Id, type, dataId));

            }

            return ret;
        }

        /// <summary>
        /// Retrieve the next item to be dequeued
        /// </summary>
        /// <returns>Next Queue Item if any exist</returns>
        public IDrpQueueItem Peak()
        {
            DrpServerQueueEnqueuedItem enqueuedItem = null;
            IDrpQueueItem ret = null;
            try
            {
                Guid queueItemId = Guid.Empty;

                enqueuedItem = this.ServerQueueItemsQueueOrder.FirstOrDefault();

                if (null != enqueuedItem)
                {
                    ret = this.ServerQueueItems.Find(enqueuedItem.QueueItemId);
                }
            }
            catch (System.Exception ex)
            {
                this.WriteAppLogEntry(
                    "[Peak].[Failure]",
                    "[Error Executing Peek()]", ex);
                Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.Peek", ex);
                ret = null;
            }
            return ret;
        }

        /// <summary>
        /// Retrieved the Next Item to be Dequeued where type = argument value
        /// </summary>
        /// <param name="itemType">Type to find</param>
        /// <returns>First Queue item with Type value</returns>
        public IDrpQueueItem Peak(string itemType)
        {
            DrpServerQueueEnqueuedItem enqueuedItem = null;
            IDrpQueueItem ret = null;
            if (string.IsNullOrWhiteSpace(itemType))
            {
                ret = this.Peak();
            }
            else
            {
                try
                {
                    enqueuedItem = this.ServerQueueItemsQueueOrder.Where(sqi =>
                        sqi.ItemType.Equals(itemType,
                        StringComparison.InvariantCultureIgnoreCase
                        )).FirstOrDefault();

                    if (null != enqueuedItem)
                    {
                        ret = this.ServerQueueItems.Find(enqueuedItem.QueueItemId);
                    }
                }
                catch (System.Exception ex)
                {
                    this.WriteAppLogEntry(
                        "[Peak].[Failure]",
                        string.Format("[Type: {0}]", itemType),
                        ex);
                    Drp.DrpExceptionHandler.LogException(string.Format("DrpServerQueueDataContext.Peek type: {0}", itemType), ex);
                    ret = null;
                }
            }
            return ret;
        }

        /// <summary>
        /// Retrieves the specific queue item where queue item id equals the argument value.
        /// If the specified Queue Item has been acquired it will not be found.
        /// </summary>
        /// <param name="queueItemId">Queue Item Id to find</param>
        /// <returns>Specified Queue Item if found</returns>
        public IDrpQueueItem Peak(Guid queueItemId)
        {
            DrpServerQueueEnqueuedItem enqueuedItem = null;
            IDrpQueueItem ret = null;
            try
            {
                enqueuedItem = this.ServerQueueItemsQueueOrder.Where(sqi =>
                    sqi.QueueItemId.Equals(queueItemId)).FirstOrDefault();

                if (null != enqueuedItem)
                {
                    ret = this.ServerQueueItems.Find(enqueuedItem.QueueItemId);
                }
            }
            catch (System.Exception ex)
            {
                this.WriteAppLogEntry(
                    "[Peak].[Failure]",
                    string.Format("[queueItemId: {0}]", queueItemId.ToStringOuterBraces()),
                    ex);
                Drp.DrpExceptionHandler.LogException(string.Format("DrpServerQueueDataContext.Peek queueItemId: {0}", queueItemId.ToStringNoFormatting()), ex);
                ret = null;
            }
            return ret;
        }


        /// <summary>
        /// Retrieves a queue item with ItemId (user defined id) value specified
        /// </summary>
        /// <param name="itemId">User Item Id to find</param>
        /// <returns>First Queue item found with the given ItemId value</returns>
        public IDrpQueueItem PeakItemId(string itemId)
        {
            IDrpQueueItem queueItem = null;
            try
            {
                queueItem = this.ServerQueueItems.AsNoTracking().Where(
                    sqi => itemId.Equals(sqi.ItemId, StringComparison.OrdinalIgnoreCase)
                    ).OrderBy(sqi => sqi.Created).FirstOrDefault();
            }
            catch (System.Exception ex)
            {
                this.WriteAppLogEntry(
                    "[PeakItemId].[Failure]",
                    string.Format("[itemId: {0}]", itemId),
                    ex);
                Drp.DrpExceptionHandler.LogException(
                    string.Format("DrpServerQueueDataContext.PeakItemId itemId: {0}", itemId), ex);
                queueItem = null;
            }
            return queueItem;
        }

        #region Acquire Private Methods

        /// <summary>
        /// Return ServerQueueItems in Queue Order (first in first out)
        /// </summary>
        private IOrderedQueryable<DrpServerQueueEnqueuedItem> ServerQueueItemsQueueOrder
        {
            get { return this.ServerQueueEnqueuedItems.AsNoTracking().OrderBy(sqei => sqei.Created); }
        }

        /// <summary>
        /// Ensures that the acquire was actually successful.
        /// </summary>
        /// <param name="queueStateId">Id for the Aquired Table Entry</param>
        /// <param name="queueItemId">Id of the Queue Item</param>
        /// <param name="acquirerId">AcquiredBy value if this acquire was actually successful</param>
        /// <returns>The Queue Item if acquire was actually successful, otherwise null</returns>
        private IDrpQueueItem EnsureAcquireSucceeded(Guid queueStateId, Guid queueItemId, string acquirerId)
        {
            IDrpQueueItem queueItem = null;

            // Guid.Empty for the ID means the stored procedure failed so no acquire here.
            if (false == Guid.Empty.Equals(queueStateId))
            {
                // Retrieved the acquired queue item 
                queueItem = this.ServerQueueItems.AsNoTracking()
                    .Where(sqi => sqi.Id == queueItemId && sqi.AcquiredBy.Equals(acquirerId, StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();

                if (null == queueItem)
                {
                    // If null was returned from query, then another process actually acquired
                    // Duplicate Acquire success from stored procedure but this one actually
                    //      failed despite the transactions in the stored procedure.
                    // That means there is an acquired state record that needs to be deleted.
                    try
                    {
                        DrpServerQueueAcquiredItem orphanedAcquire = this.ServerQueueAcquiredItems.Where(sqai => sqai.Id == queueStateId).FirstOrDefault();
                        if (null != orphanedAcquire)
                        {
                            this.ServerQueueAcquiredItems.Remove(orphanedAcquire);
                            this.WriteAppLogEntry(
                                "[Acquire].[Failure-Orphaned]",
                                string.Format(" Deleted Succeeded [@itemId: {0}, Id: {1},  acquiredBy: {2}]",
                                    queueItemId.ToStringDashes(), queueStateId.ToStringDashes(), acquirerId));
                            this.SaveChanges();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        this.WriteAppLogEntry(
                            "[Acquire].[Failure-Orphaned]",
                            string.Format(" Deleted Failed [@itemId: {0}, Id: {1},  acquiredBy: {2}]",
                                queueItemId.ToStringDashes(), queueStateId.ToStringDashes(), acquirerId),
                            ex);
                    }
                }
            }

            return queueItem;
        }


         #endregion

        /// <summary>
        /// <summary>
        /// Marks a queue item as acquired. An acquired item can only be dequeued by the same acquirerId value.
        /// Acquire steps:
        ///   - Retrieve item from the queue table.
        ///   Start Transaction
        ///     - Add item to Acquired table
        ///     - Remove item from queue table
        ///   End Transaction (rollback on failure).
        /// </summary>
        /// </summary>
        /// <param name="acquirerId">Required</param>
        /// <returns>Acquired Item on success, null on failure</returns>
        public IDrpQueueItem Acquire(string acquirerId)
        {
            return this.Acquire(acquirerId, string.Empty);
        }

        /// <summary>
        /// Marks a queue item as acquired. An acquired item can only be dequeued by the same acquirerId value.
        /// Acquire steps:
        ///   - Retrieve item from the queue table.
        ///   Start Transaction
        ///     - Add item to Acquired table
        ///     - Remove item from queue table
        ///   End Transaction (rollback on failure).
        /// </summary>
        /// <param name="acquirerId">Required</param>
        /// <param name="itemType">Queue Item type (category) to attempt aquire</param>
        /// <returns>Acquired Item on success, null on failure</returns>
        public IDrpQueueItem Acquire(string acquirerId, string itemType)
        {
            IDrpQueueItem queueItem = null;
            Guid queueItemId = Guid.Empty;
            Guid queueStateId = Guid.Empty;

            /*

                CREATE PROCEDURE [dbo].[spQueueItemAcquire]
	                @itemType NVARCHAR(255) = NULL,
                    @acquiredBy NVARCHAR(255) = NULL,
	                @retryAttempts INT = 10
 
            */

            using (SqlCommand sqlCommand = new SqlCommand(
                "exec dbo.spQueueItemAcquire @itemType, @acquiredBy, @retryAttempts"))
            {

                sqlCommand.Connection = (SqlConnection)this.Database.Connection;
                if (sqlCommand.Connection.State != System.Data.ConnectionState.Open)
                {
                    sqlCommand.Connection.Open();
                }

                sqlCommand.Parameters.AddWithValue("@itemType", itemType);
                sqlCommand.Parameters.AddWithValue("@acquiredBy", acquirerId);
                sqlCommand.Parameters.AddWithValue("@retryAttempts", GetAcquireRetryAttempts());

                try
                {
                    // If nothing returned, then Acquire failed
                    using (SqlDataReader sqlreader = sqlCommand.ExecuteReader())
                    {
                        while (sqlreader.Read())
                        {
                            queueStateId = (Guid)sqlreader["Id"];
                            queueItemId = (Guid)sqlreader["QueueItemId"];
                            // There should only be one but just in case... exit here
                            break;
                        }

                        if (sqlreader.Read())
                        {
                            this.WriteAppLogEntry(
                                "[Acquire].[Issue]",
                                string.Format("sqlReader return more than one row: [@itemType: {0},  @acquiredBy: {1}]", itemType, acquirerId));
                        }
                    }

                }
                catch (System.Exception ex)
                {
                    this.WriteAppLogEntry(
                        "[Acquire].[Failure]",
                        string.Format("[@itemId: {0},  @aquirerId: {1}]", queueItemId.ToStringDashes(), acquirerId),
                        ex);
                    queueItem = null;
                }


            }

            // Ensure the Acquire Succeeded
            queueItem = EnsureAcquireSucceeded(queueStateId, queueItemId, acquirerId);

            if (null != queueItem)
            {
                this.WriteQueueLogEntry(
                    Guid.Empty,
                    queueStateId,
                    queueItem.Id,
                    "[Acquire].[Success]",
                    string.Format("[Acquire Item [Id: {0} Type: {1}, AcquiredBy: {2}]",
                        queueItem.Id, queueItem.ItemType, queueItem.AcquiredBy));

            }

            return queueItem;
        }

        /// <summary>
        /// Marks a queue item as acquired. An acquired item can only be dequeued by the same acquirerId value.
        /// Acquire steps:
        ///   - Retrieve item from the queue table.
        ///   Start Transaction
        ///     - Add item to Acquired table
        ///     - Remove item from queue table
        ///   End Transaction (rollback on failure).
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="queueItemId">Id of queue item to release</param>
        /// <returns>Acquired Item on success, null on failure</returns>
        public IDrpQueueItem Acquire(string acquirerId, Guid queueItemId)
        {
            Guid queueStateId = Guid.Empty;
            IDrpQueueItem queueItem = null;

            /*

                CREATE PROCEDURE [dbo].[spQueueItemAcquireSpecific]
	                @queueItemId UniqueIdentifier = NULL,
                    @acquiredBy NVARCHAR(255) = NULL

            */

            using (SqlCommand sqlCommand = new SqlCommand(
                "exec dbo.spQueueItemAcquireSpecific @queueItemId, @acquiredBy"))
            {

                sqlCommand.Connection = (SqlConnection)this.Database.Connection;
                if (sqlCommand.Connection.State != System.Data.ConnectionState.Open)
                {
                    sqlCommand.Connection.Open();
                }

                sqlCommand.Parameters.AddWithValue("@queueItemId", queueItemId);
                sqlCommand.Parameters.AddWithValue("@acquiredBy", acquirerId);

                try
                {
                    // If nothing returned, then Acquire failed
                    using (SqlDataReader sqlreader = sqlCommand.ExecuteReader())
                    {
                        // Even though the data is not used, read the first row so the next If works
                        while (sqlreader.Read())
                        {
                            queueStateId = (Guid)sqlreader["Id"];

                            queueItem = this.ServerQueueHistoryItems.AsNoTracking()
                                .Where(sqhi => sqhi.Id == queueItemId).FirstOrDefault();

                            // There should only be one but just in case... exit here
                            break;
                        }

                        // If there is more than one row to read, then something is wrong.
                        // Log the issue but do not stop the queue processing.
                        if (sqlreader.Read())
                        {
                            this.WriteAppLogEntry(
                                "[Acquire].[Issue]",
                                string.Format("sqlReader return more than one row: [@itemId: {0},  @acquiredBy: {1}]", queueItemId.ToStringDashes(), acquirerId));
                        }
                    }

                }
                catch (System.Exception ex)
                {
                    this.WriteAppLogEntry(
                        "[Acquire].[Failure]",
                        string.Format("[@itemId: {0},  @aquirerBy: {1}]", queueItemId.ToStringDashes(), acquirerId),
                        ex);
                    queueItem = null;
                }
            }

            // Ensure the Acquire Succeeded
            queueItem = EnsureAcquireSucceeded(queueStateId, queueItemId, acquirerId);

            if (null != queueItem)
            {
                this.WriteQueueLogEntry(
                    Guid.Empty,
                    queueStateId,
                    queueItem.Id,
                    "[Acquire].[Success]",
                    string.Format("[Acquire Item [Id: {0} Type: {1}, AcquiredBy: {2}]",
                        queueItem.Id.ToStringDashes(), queueItem.ItemType, queueItem.AcquiredBy));
            }

            return queueItem;
        }

        /// <summary>
        /// Releases a previously acquired queue item
        /// Release steps:
        ///   - Retrieve State item from the Acquired table.
        ///   Start Transaction
        ///     - Add State Item to Enqueue table
        ///     - Remove State Item from Acquired table
        ///   End Transaction (rollback on failure).
        /// </summary>
        /// <param name="acquiredBy">Required - Must match database value - Case Sensitive</param>
        /// <param name="queueItemId">Id of queue item to release</param>
        /// <returns>Released Item on success, null on failure</returns>
        public IDrpQueueItem Release(string acquiredBy, Guid queueItemId)
        {
            DrpServerQueueItem queueItem = null;
            Guid queueStateId = Guid.Empty;

            /*

            CREATE PROCEDURE [dbo].[spQueueItemRelease]
                @queueItemId UniqueIdentifier,
                @acquiredBy NVARCHAR(255) = NULL


            */

            using (SqlCommand sqlCommand = new SqlCommand(
                "exec dbo.spQueueItemRelease @queueItemId, @acquiredBy"))
            {

                sqlCommand.Connection = (SqlConnection)this.Database.Connection;
                if (sqlCommand.Connection.State != System.Data.ConnectionState.Open)
                {
                    sqlCommand.Connection.Open();
                }

                sqlCommand.Parameters.AddWithValue("@queueItemId", queueItemId);
                sqlCommand.Parameters.AddWithValue("@acquiredBy", acquiredBy);

                try
                {
                    // If nothing returned, then Dequeue failed
                    using (SqlDataReader sqlreader = sqlCommand.ExecuteReader())
                    {
                        // Even though the data is not used, read the first row so the next If works
                        while (sqlreader.Read())
                        {
                            queueStateId = (Guid)sqlreader["Id"];
                            // There should only be one but just in case... exit here
                            break;
                        }

                        // If there is more than one row to read, then something is wrong.
                        // Log the issue but do not stop the queue processing.
                        if (sqlreader.Read())
                        {
                            this.WriteAppLogEntry(
                                "[Release].[Issue]",
                                string.Format("sqlReader return more than one row: [@itemId: {0},  @acquiredBy: {1}]", queueItemId.ToStringDashes(), acquiredBy));
                        }
                    }

                }
                catch (System.Exception ex)
                {
                    this.WriteAppLogEntry(
                        "[Release].[Failure]",
                        string.Format("[@itemId: {0},  @aquirerId: {1}]", queueItemId.ToStringDashes(), acquiredBy),
                        ex);
                    queueItem = null;
                }
            }

            if (false == Guid.Empty.Equals(queueStateId))
            {
                queueItem = this.ServerQueueItems.AsNoTracking()
                    .Where(sqi => sqi.Id == queueItemId).FirstOrDefault();

            }

            if (null != queueItem)
            {
                this.WriteQueueLogEntry(
                    Guid.Empty,
                    queueStateId,
                    queueItem.Id,
                    "[Release].[Success]",
                    string.Format("[Release Item [Id: {0} Type: {1}, AcquiredBy: {2}]",
                        queueItem.Id, queueItem.ItemType, queueItem.AcquiredBy));

            }

            return queueItem;
        }

        /// <summary>
        /// Dequeues an Item. The Item must have been previously Acquired.
        /// Dequeue steps:
        ///   - Retrieve item from the Acquired table.
        ///   Start Transaction
        ///     - Add item to History table
        ///     - Remove item from Acquired table
        ///   End Transaction (rollback on failure).
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="queueItemId">Id of queue item to release</param>
        /// <returns>Dequeued Item on success, null on failure</returns>
        public IDrpQueueItem Dequeue(string acquirerId, Guid queueItemId)
        {
            DrpServerQueueHistoryItem queueItem = null;
            Guid queueStateId = Guid.Empty;

            /*

            CREATE PROCEDURE [dbo].[spQueueItemDequeue]
                @queueItemId UniqueIdentifier,
                @acquiredBy NVARCHAR(255) = NULL

            */

            using (SqlCommand sqlCommand = new SqlCommand(
                "exec dbo.spQueueItemDequeue @queueItemId, @acquiredBy"))
            {

                sqlCommand.Connection = (SqlConnection)this.Database.Connection;
                if (sqlCommand.Connection.State != System.Data.ConnectionState.Open)
                {
                    sqlCommand.Connection.Open();
                }

                sqlCommand.Parameters.AddWithValue("@queueItemId", queueItemId);
                sqlCommand.Parameters.AddWithValue("@acquiredBy", acquirerId);

                try
                {
                    // If nothing returned, then Dequeue failed
                    using (SqlDataReader sqlreader = sqlCommand.ExecuteReader())
                    {
                        // Even though the data is not used, read the first row so the next If works
                        while (sqlreader.Read())
                        {
                            queueStateId = (Guid)sqlreader["Id"];
                            // There should only be one but just in case... exit here
                            break;
                        }

                        // If there is more than one row to read, then something is wrong.
                        // Log the issue but do not stop the queue processing.
                        if (sqlreader.Read())
                        {
                            this.WriteAppLogEntry(
                                "[Dequeue].[Issue]",
                                string.Format("sqlReader return more than one row: [@itemId: {0},  @acquiredBy: {1}]", queueItemId.ToStringDashes(), acquirerId));
                        }
                    }

                }
                catch (System.Exception ex)
                {
                    this.WriteAppLogEntry(
                        "[Dequeue].[Failure]",
                        string.Format("[@itemId: {0},  @aquirerBy: {1}]", queueItemId.ToStringDashes(), acquirerId),
                        ex);
                    queueItem = null;
                }
            }

            if (false == Guid.Empty.Equals(queueStateId))
            {
                queueItem = this.ServerQueueHistoryItems.AsNoTracking()
                    .Where(sqhi => sqhi.Id == queueItemId).FirstOrDefault();
            }

            if (null != queueItem)
            {
                this.WriteQueueLogEntry(
                    Guid.Empty,
                    queueStateId,
                    queueItem.Id,
                    "[Dequeue].[Success]",
                    string.Format("[Dequeue Item [Id: {0} Type: {1}, AcquiredBy: {2}]",
                        queueItem.Id.ToStringDashes(), queueItem.ItemType, queueItem.AcquiredBy));

            }

            return queueItem;
        }


        #endregion

        #region Queue Maintenance methods

        /// <summary>
        /// Returns ServerQueueItems that were acquired longer than staleTimePeriod and have the
        ///     specified itemType value.
        ///     if staleTimePeriod is less than TimeSpan.Zero,
        ///         all ServerQueueItems of the specified itemType value are returned.
        ///     if true == string.IsNullOrWhiteSpace(itemType) then item type is removed from the filter.
        /// </summary>
        /// <param name="itemType">ItemType value to include. If null or whitespace then include all</param>
        /// <param name="staleTimePeriod">Time period subracted from UctNow for aquired value filtering</param>
        /// <returns>All Queue Items that are stale based on the argument values</returns>
        public IList<Guid> GetStaleServerQueueAcquiredItems(string itemType, TimeSpan staleTimePeriod)
        {
            List<Guid> ret = new List<Guid>();
            try
            {
                if (TimeSpan.Zero >= staleTimePeriod)
                {
                    // TimeSpan less than zero so no time filtering

                    if (string.IsNullOrWhiteSpace(itemType))
                    {
                        // All Type Values
                        ret = this.ServerQueueAcquiredItems.AsNoTracking().Select(sqi => sqi.QueueItemId).ToList();
                    }
                    else
                    {
                        // Specific type values
                        ret = this.ServerQueueAcquiredItems.AsNoTracking().Where(
                            sqi => itemType.Equals(sqi.ItemType, StringComparison.InvariantCultureIgnoreCase))
                            .Select(sqi => sqi.QueueItemId).ToList();
                    }
                }
                else
                {
                    // Stale Period to check (time filtering)
                    DateTimeOffset testDateTime = DateTimeOffset.UtcNow.Subtract(staleTimePeriod);

                    if (string.IsNullOrWhiteSpace(itemType))
                    {
                        // All ItemType values
                        ret = this.ServerQueueAcquiredItems.AsNoTracking().Where(sqi => sqi.Acquired >= testDateTime)
                            .Select(sqi => sqi.QueueItemId).ToList();
                    }
                    else
                    {
                        // Specific ItemType value
                        ret = this.ServerQueueAcquiredItems.AsNoTracking().Where(
                            sqi => sqi.Acquired >= testDateTime
                            && itemType.Equals(sqi.ItemType, StringComparison.InvariantCultureIgnoreCase))
                            .Select(sqi => sqi.QueueItemId).ToList();
                    }
                }
            }
            catch (System.Exception ex)
            {
                ret = new List<Guid>();
                this.WriteAppLogEntry(
                    "[GetStaleServerQueueAcquiredItems].[Failure]",
                    string.Format("[GetStaleServerQueueAcquiredItems: itemType: {0} Exception: {1}]", itemType),
                    ex);
                Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.GetStaleServerQueueAcquiredItems", ex);
            }

            return ret;
        }

        /// <summary>
        /// Add an acquired item back into the queue (un-Acquire)
        /// This
        ///     - removes acquired and acquired by values
        ///     - keeps the Queue Item Id and Created values
        ///     - increments that StaleAcquireCount value
        ///     Queue Item changes are in a transaction so all or nothing
        /// </summary>
        /// <param name="queueItemId">Id of item to move back to the queue</param>
        /// <returns>IDrpQueryItem of the requeued item</returns>
        public IDrpQueueItem RequeueAcquiredItem(Guid queueItemId)
        {
            string acquirerId = string.Empty;
            DrpServerQueueItem queueItem = null;
            DrpServerQueueAcquiredItem toRelease = null;
            DrpServerQueueEnqueuedItem toEnqueue = null;

            try
            {
                toRelease = this.ServerQueueAcquiredItems.AsNoTracking().Where(
                    sqi => sqi.QueueItemId.Equals(queueItemId)).FirstOrDefault();

                if (null != toRelease)
                {
                    acquirerId = toRelease.AcquiredBy;
                    queueItem = this.ServerQueueItems.Find(queueItemId);
                }

            }
            catch (System.Exception ex)
            {
                this.WriteAppLogEntry(
                    "[RequeueAquiredItem].[Failure]",
                    string.Format(
                        "[Initial Select. acquireId: {0} queueItemId: {1}]",
                        acquirerId,
                        queueItemId.ToStringOuterBraces()),
                    ex);
                Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.RequeueAquiredItem", ex);
                toRelease = null;
                queueItem = null;
            }

            if (null != queueItem)
            {
                using (DbContextTransaction transaction = this.Database.BeginTransaction())
                {
                    try
                    {
                        queueItem.Release();
                        toEnqueue = new DrpServerQueueEnqueuedItem(toRelease);
                        // If the add failed, then the item was acquired after the original DrpServerQueryItem was retrieved.
                        this.ServerQueueEnqueuedItems.Add(toEnqueue);
                        this.ServerQueueAcquiredItems.Remove(toRelease);
                        this.SaveChanges();
                        transaction.Commit();
                        this.WriteQueueLogEntry(
                            toRelease.Id,
                            toEnqueue.Id,
                            queueItemId,
                            "[RequeueAquiredItem].[Success]",
                            string.Format(
                                "[RequeueAquiredItem: acquireId: {0} queueItemId: {1}]",
                                acquirerId,
                                queueItemId.ToStringOuterBraces()));
                    }
                    catch (System.Exception ex)
                    {
                        transaction.Rollback();
                        this.WriteAppLogEntry(
                            "[RequeueAquiredItem].[Failure]",
                            string.Format(
                                "[RolledBack: acquireId: {0} queueItemId: {1}",
                                acquirerId,
                                queueItemId.ToStringOuterBraces()),
                            ex);
                        Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.RequeueAquiredItem", ex);
                        queueItem = null;
                    }
                }

            }
            return queueItem;
        }

        /// <summary>
        /// Requeue Acquired Items meeting the itemType and staleTimePeriod criteria
        /// Add an acquired item back into the queue (un-Acquire)
        /// This
        ///     - removes acquired and acquired by values
        ///     - keeps the Queue Item Id and Created values
        ///     - increments that StaleAcquireCount value
        /// </summary>
        /// <param name="itemType">ItemType value to include. If null or whitespace then include all</param>
        /// <param name="staleTimePeriod">Time period subracted from UctNow for aquired value filtering</param>
        /// <returns>Tuple of int values countSuccess and countAll. If countSuccess == countAll then all 
        ///             the items meeting the criteria were requeued
        /// </returns>
        public Tuple<int, int> RequeueStaleAcquiredItems(string itemType, TimeSpan staleTimePeriod)
        {
            int counterAll = 0;
            int countSuccess = 0;

            try
            {
                foreach (Guid queueitemId in this.GetStaleServerQueueAcquiredItems(itemType, staleTimePeriod))
                {
                    counterAll++;
                    if (null != this.RequeueAcquiredItem(queueitemId))
                    {
                        countSuccess++;
                    }
                }
            }
            catch (System.Exception  ex)
            {
                this.WriteAppLogEntry(
                    "[RequeueStaleAcquired].[Failure]",
                    string.Format(
                        "[RequeueStaleAcquired: acquireId: {0} queueItemId: {1}]",
                        itemType,
                        staleTimePeriod.ToString(@"'['ddd'].['hh\:mm\:ss']'")),
                    ex);
                Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.RequeueStaleAcquired", ex);
                countSuccess = -1;
                counterAll = -1;
            }

            return new Tuple<int, int>(countSuccess, counterAll);
        }

        /// <summary>
        /// Returns ServerQueueItems that were acquired longer than staleTimePeriod and have the
        ///     specified itemType value.
        ///     if staleTimePeriod is less than TimeSpan.Zero, TimeSpan is subtracted from Zero to invert
        ///     if true == string.IsNullOrWhiteSpace(itemType) then item type is removed from the filter.
        /// </summary>
        /// <param name="itemType">ItemType value to include. If null or whitespace then include all</param>
        /// <param name="staleTimePeriod">Time period subracted from UctNow for aquired value filtering</param>
        /// <returns>All Queue Items that are stale based on the argument values</returns>
        public IList<Guid> GetStaleServerQueueEnqueuedItems(string itemType, TimeSpan staleTimePeriod)
        {
            List<Guid> ret = new List<Guid>();
            try
            {
                if (TimeSpan.Zero > staleTimePeriod)
                {
                    staleTimePeriod = TimeSpan.Zero.Subtract(staleTimePeriod);
                }

                // Stale Period to check (time filtering)
                DateTimeOffset testDateTime = DateTimeOffset.UtcNow.Subtract(staleTimePeriod);

                if (string.IsNullOrWhiteSpace(itemType))
                {
                    // All ItemType values
                    ret = this.ServerQueueEnqueuedItems.AsNoTracking().Where(sqi => sqi.Created >= testDateTime)
                        .Select(sqi => sqi.QueueItemId).ToList();
                }
                else
                {
                    // Specific ItemType value
                    ret = this.ServerQueueEnqueuedItems.AsNoTracking().Where(
                        sqi => sqi.Created >= testDateTime
                        && itemType.Equals(sqi.ItemType, StringComparison.InvariantCultureIgnoreCase))
                        .Select(sqi => sqi.QueueItemId).ToList();
                }
            }
            catch (System.Exception ex)
            {
                ret = new List<Guid>();
                this.WriteAppLogEntry(
                    "[GetStaleServerQueueEnqueuedItems].[Failure]",
                    string.Format("[GetStaleServerQueueEnqueuedItems: itemType: {0} Exception: {1}]", itemType),
                    ex);
                Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.GetStaleServerQueueEnqueuedItems", ex);
            }

            return ret;
        }

        #endregion

    }
}
