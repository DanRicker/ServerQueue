/*
    Copyright 2016 Peoplutions
*/

namespace Drp.SeverQueueData.DAL
{

    #region Using Statements

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration.Conventions;

    using Drp;
    using Drp.SeverQueueData.Models;
    using Drp.Types;

    using AppSettings = Drp.Properties.Settings;

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

        public static readonly string DefaultConnectionString = AppSettings.Default.DefaultConnectionString;

        /// <summary>
        /// TODO: Config setting
        /// </summary>
        /// <returns></returns>
        public static int GetAcquireRetryAttempts()
        {
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
        internal static DrpServerQueueDataContext NewDrpServerQueueDataContext(string connString = null)
        {
            return new DrpServerQueueDataContext(EnsureConnectionString(connString));
        }

        /// <summary>
        /// Get the connections string to use for new instance creation. If connection string argument is blank
        /// returns the defauilt connection string
        /// </summary>
        /// <param name="connectionStringArg">Connection string argument</param>
        /// <returns>connection string to use for the new instance</returns>
        private static string EnsureConnectionString(string connectionStringArg)
        {
            string ret = connectionStringArg;
            if (string.IsNullOrWhiteSpace(ret))
            {
                ret = DrpServerQueueDataContext.DefaultConnectionString;
            }

            return ret;
        }

        #endregion

        #region Database Tables


        public DbSet<DrpServerQueueItem> ServerQueueItems { get; set; }
        public DbSet<DrpServerQueueHistoryItem> ServerQueueHistoryItems { get; set; }


        /// <summary>
        /// Three Tables to hold the queue items in the 3 states.
        ///     - ServerQueueItems: Items added to the queue
        ///     - ServerQueueAquiredItems: Items that have been acquired
        ///     - ServerQueueHistoryItems: History table of queue items that have passed through this queue
        /// </summary>
        public DbSet<DrpServerQueueEnqueuedItem> ServerQueueEnqueuedItems { get; set; }
        public DbSet<DrpServerQueueAcquiredItem> ServerQueueAcquiredItems { get; set; }
        public DbSet<DrpServerQueueDequeuedItem> ServerQueueDequeuedItems { get; set; }

        /// <summary>
        /// Each Queue Action is recorded in the Server Queue Logs table
        /// </summary>
        public DbSet<DrpServerQueueLogEntry> ServerQueueLogs { get; set; }


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
                    WriteAppLogEntry(
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
        /// Validate AcquirerId value. Currently only not blank
        /// </summary>
        /// <param name="acquirerId">value to validate</param>
        /// <param name="action">Queue Action wanting validation</param>
        /// <param name="queueItemId">queueItemId of queue item being acted on</param>
        /// <returns>True if the acquirerId value is valid, otherwise false</returns>
        private bool ValidateAquireId(string acquirerId, string action, Guid queueItemId)
        {
            bool ret = (false == string.IsNullOrWhiteSpace(acquirerId));
            if (false == ret)
            {
                WriteAppLogEntry(
                    string.Format("[{0}].[Failure]", action),
                    string.Format("[aquirerId is invalid: queueItemId: {0}]", queueItemId.ToStringOuterBraces()));
            }
            return ret;
        }

        /// <summary>
        /// Override OnModelCreating to complete mapping
        ///  - Inherited Properties need to be explicitly mapped
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // -- Not sure why this is not needed now. They all inherit now..
            //modelBuilder.Entity<DrpServerQueueAcquiredItem>().Map(m =>
            //{
            //    m.MapInheritedProperties();
            //    m.ToTable("ServerQueueAquiredItem");
            //});

            //modelBuilder.Entity<DrpServerQueueDequeuedItem>().Map(m =>
            //{
            //    m.MapInheritedProperties();
            //    m.ToTable("ServerQueueDequeuedItem");
            //});

            //modelBuilder.Entity<DrpServerQueueHistoryItem>().Map(m =>
            //{
            //    m.MapInheritedProperties();
            //    m.ToTable("ServerQueueHistoryItem");
            //});

            base.OnModelCreating(modelBuilder);
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
            this.Configuration.EnsureTransactionsForFunctionsAndCommands = true;
            this.Configuration.LazyLoadingEnabled = false;
            this.Configuration.ProxyCreationEnabled = true;
            this.Configuration.ValidateOnSaveEnabled = true;

            // Static SetInitializer for DbContext Database
            Database.SetInitializer<DrpServerQueueDataContext>(new CreateDatabaseIfNotExists<DrpServerQueueDataContext>());
            if (this.Database.CreateIfNotExists())
            {
                WriteAppLogEntry(
                    "DrpServerQueueDataContext_ctor",
                    string.Format("Success - Database initialized using connection string: {0}", connectionString));
            }
            else
            {
                WriteAppLogEntry(
                    "DrpServerQueueDataContext_ctor",
                    string.Format("Success (Database Existed) - Database initialized using connection string: {0}", connectionString));
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
                    return ServerQueueEnqueuedItems.Count();
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
                    return ServerQueueAcquiredItems.Count();
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
                return this.ServerQueueEnqueuedItems.Where(sqi => sqi.ItemType.Equals(itemType, StringComparison.Ordinal)).Count();
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
                return this.ServerQueueAcquiredItems.Where(sqi => sqi.ItemType.Equals(itemType, StringComparison.Ordinal)).Count();
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
            using (DbContextTransaction transaction = this.Database.BeginTransaction())
            {
                try
                {
                    Guid queueItemId = Guid.NewGuid();

                    // Constructor handles MaxLength values for type and dataId
                    ret = new DrpServerQueueItem(type, dataId, data, metadata)
                    { Id = queueItemId };

                    DrpServerQueueEnqueuedItem queueItem = new DrpServerQueueEnqueuedItem()
                    {
                        Id = Guid.NewGuid(),
                        ItemType = ret.ItemType, // if truncated get what is saved
                        QueueItemId = queueItemId,
                        Created = DateTimeOffset.UtcNow
                    };
                    this.ServerQueueItems.Add(ret);
                    this.ServerQueueEnqueuedItems.Add(queueItem);
                    this.SaveChanges();
                    transaction.Commit();

                    this.WriteQueueLogEntry(
                        Guid.Empty,
                        queueItem.Id,
                        ret.Id,
                        "[Enqueue].[Success]",
                        string.Format("[Enqueue Id: {0} Item [type: {1}, dataId: {2}]", ret.Id, type, dataId));

                }
                catch (System.Exception ex)
                {
                    transaction.Rollback();
                    this.WriteAppLogEntry(
                        "[Enqueue].[Failure]",
                        string.Format("[RolledBack Item [type: {1}, dataId: {2}]]", type, dataId),
                        ex);
                    Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.Enqueue", ex);
                }
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

                enqueuedItem = this.ServerQueueEnqueuedItems.OrderBy(sqi => sqi.Created).FirstOrDefault();

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
                    enqueuedItem = this.ServerQueueEnqueuedItems.Where(sqi =>
                        sqi.ItemType.Equals(itemType,
                        StringComparison.InvariantCultureIgnoreCase
                        )).OrderBy(sqi => sqi.Created).FirstOrDefault();

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
                enqueuedItem = this.ServerQueueEnqueuedItems.Where(sqi =>
                    sqi.QueueItemId.Equals(queueItemId)).OrderBy(sqi => sqi.Created).FirstOrDefault();

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
                queueItem = this.ServerQueueItems.Where(
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
        /// Retrieved the actual QueueItem for an Acquired Item entry
        /// Context save is a SaveChangesAsync(). The updated Entries are lower priority than
        ///     the actual Queue Processing
        /// </summary>
        /// <param name="acquiredItem">Owner of the Queue Item</param>
        /// <returns>the retrieved QueueItem marked as acquired and saved to DB</returns>
        private IDrpQueueItem SetQueueItemAcquired(DrpServerQueueAcquiredItem acquiredItem)
        {
            DrpServerQueueItem queueItem = null;
            if(null != acquiredItem && false == acquiredItem.QueueItemId.Equals(Guid.Empty))
            {
                try
                {
                    queueItem = this.ServerQueueItems.Find(acquiredItem.QueueItemId);
                    if (null != queueItem)
                    {
                        queueItem.Acquire(acquiredItem.AcquiredBy);
                        this.SaveChanges();
                    }
                    else
                    {
                        this.WriteAppLogEntry(
                            "[GetAcquiredQueueItem].[Failure]",
                            string.Format("Queue Item Not found: [acquiredItem [id: {0}, queueItemId: {1}]", acquiredItem.Id, acquiredItem.QueueItemId));
                    }
                }
                catch (System.Exception ex)
                {
                    this.WriteAppLogEntry(
                        "[GetAcquiredQueueItem].[Failure]",
                        string.Format("[acquiredItem [id: {0}, queueItemId: {1}]", acquiredItem.Id, acquiredItem.QueueItemId),
                        ex);
                    Drp.DrpExceptionHandler.LogException(
                        string.Format(
                            "DrpServerQueueDataContext.GetAcquiredQueueItem [acquiredItem [id: {0}, queueItemId: {1}]",
                            acquiredItem.Id,
                            acquiredItem.QueueItemId),
                        ex);
                    queueItem = null;
                }
            }
            return queueItem;
        }

        /// <summary>
        /// Tranaction Wrapped Actual Acquire Action.
        /// 
        ///   Start Transaction
        ///     - Add item to Acquired table
        ///     - Remove item from queue table
        ///   End Transaction (rollback on failure).
        /// </summary>
        /// <param name="enqueuedItem">The enqueued Item to be acquired</param>
        /// <param name="acquirerId">The User Defined aquirerId value. Used to filter Dequeueing items</param>
        /// <returns>The Acquired Item on successfull transaction.Commit() otherwise null</returns>
        private DrpServerQueueAcquiredItem AttemptAcquire(DrpServerQueueEnqueuedItem enqueuedItem, string acquirerId)
        {
            DrpServerQueueAcquiredItem acquiredItem = null;

            if (null != enqueuedItem)
            {
                // Create the acquired item and set the acquired values
                acquiredItem = new DrpServerQueueAcquiredItem(enqueuedItem);
                acquiredItem.AcquiredBy = acquirerId;
                acquiredItem.Acquired = DateTimeOffset.UtcNow;

                // Start the transaction
                using (DbContextTransaction transaction = this.Database.BeginTransaction())
                {
                    try
                    {
                        // One possible optimization is to move this to be a stored procedure created in the database as part of
                        // the code first rollout. That would place the exception/error handling on the SQL server where the SQL server
                        // could return null after handling and logging the issue.
                        // I don't know how much this would improve the queue operations. There would still be a line of consumers trying
                        // to get that next free item.

                        // If the Remove(enqueuedItem) failed, then the item was acquired prior to this attempt.
                        this.ServerQueueEnqueuedItems.Remove(enqueuedItem);
                        this.ServerQueueAcquiredItems.Add(acquiredItem);
                        this.SaveChanges();

                        transaction.Commit();

                        // Log the success
                        this.WriteQueueLogEntry(
                            enqueuedItem.Id,
                            acquiredItem.Id,
                            enqueuedItem.QueueItemId,
                            "[Acquire].[Success]",
                            string.Format(
                                "[Acquire: acquireId: {0} queueItemId: {1}]",
                                acquirerId,
                                enqueuedItem.QueueItemId.ToStringOuterBraces()));
                    }
                    catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
                    {
                        transaction.Rollback();
                        this.WriteAppLogEntry(
                            "[Acquire].[ConcurrencyFailure]",
                            string.Format(
                                "[RolledBack: acquireId: {0} queueItemId: {1}]",
                                acquirerId,
                                enqueuedItem.QueueItemId.ToStringOuterBraces()));
                        acquiredItem = null;
                    }
                    catch (System.Exception ex)
                    {
                        transaction.Rollback();
                        this.WriteAppLogEntry(
                            "[Acquire].[Failure]",
                            string.Format(
                                "[RolledBack: acquireId: {0} queueItemId: {1}]",
                                acquirerId,
                                enqueuedItem.QueueItemId.ToStringOuterBraces()),
                            ex);
                        Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.Acquire", ex);
                        acquiredItem = null;
                    }
                }
            }
            return acquiredItem;
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
        private DrpServerQueueAcquiredItem AttemptAcquire(string acquirerId, Guid queueItemId)
        {
            DrpServerQueueAcquiredItem acquiredItem = null;
            DrpServerQueueEnqueuedItem enqueuedItem = null;

            try
            {
                // There should only be one item in the Enqueued table but just incase..
                // TODO: Maintenance item Orphaned QueueItems
                enqueuedItem = this.ServerQueueEnqueuedItems.OrderBy(sqei => sqei.Created)
                    .Where(sqei => sqei.QueueItemId.Equals(queueItemId)).FirstOrDefault();

                if (null != enqueuedItem)
                {
                    acquiredItem = this.AttemptAcquire(enqueuedItem, acquirerId);
                }
            }
            catch (System.Exception ex)
            {
                this.WriteAppLogEntry(
                    "[Acquire].[Failure]",
                    string.Format(
                        "[Initial Select. acquireId: {0} queueItemId: {1}]",
                        acquirerId,
                        queueItemId.ToStringOuterBraces()),
                    ex);
                Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.Acquire", ex);
                acquiredItem = null;
            }
            return acquiredItem;
        }



        /// <summary>
        /// Attampts to Mark a queue item as acquired. An acquired item can only be dequeued by the same acquirerId value.
        /// Acquire steps:
        ///   - Retrieve item from the queue table.
        ///   Start Transaction
        ///     - Add item to Acquired table
        ///     - Remove item from queue table
        ///   End Transaction (rollback on failure).
        /// </summary>
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="itemType">Queue Item type (category) to attempt acquire. Not used to filter if blank</param>
        /// <returns>Acquired Item on success, null on failure</returns>
        private DrpServerQueueAcquiredItem AttemptAcquire(string acquirerId, string itemType)
        {
            DrpServerQueueAcquiredItem acquiredItem = null;
            DrpServerQueueEnqueuedItem enqueuedItem = null;

            try
            {
                if (string.IsNullOrWhiteSpace(itemType))
                {
                    enqueuedItem = this.ServerQueueEnqueuedItems.OrderBy(sqei => sqei.Created).FirstOrDefault();
                }
                else
                {
                    enqueuedItem = this.ServerQueueEnqueuedItems.OrderBy(sqei => sqei.Created)
                        .Where(sqi => sqi.ItemType.Equals(itemType)).FirstOrDefault();
                }

                if (null != enqueuedItem)
                {
                    acquiredItem = this.AttemptAcquire(enqueuedItem, acquirerId);
                }
            }
            catch (System.Exception ex)
            {
                this.WriteAppLogEntry(
                    "[Acquire].[Failure]",
                    string.Format(
                        "[Initial Select. acquireId: {0} itemType: {1}]",
                        acquirerId,
                        itemType),
                    ex);
                Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.Acquire", ex);
                acquiredItem = null;
            }
            return acquiredItem;
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
            DrpServerQueueAcquiredItem acquiredItem = null;
            IDrpQueueItem queueItem = null;
            int acquireAttempts = 0;
            int maxRetryAttempts = GetAcquireRetryAttempts();

            if (ValidateAquireId(acquirerId, "Acquire", Guid.Empty))
            {
                while (acquireAttempts < maxRetryAttempts && null == acquiredItem)
                {
                    acquireAttempts++;
                    acquiredItem = this.AttemptAcquire(acquirerId, itemType);
                }

                if (null != acquiredItem)
                {
                    queueItem = this.SetQueueItemAcquired(acquiredItem);
                }
            }

            if (null == queueItem)
            {
                this.WriteAppLogEntry(
                    "[Acquire].[Failure]",
                     string.Format(
                        "[Acquire: acquireId: {0} itemType: {1}]",
                        acquirerId,
                        itemType));
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
            DrpServerQueueAcquiredItem acquiredItem = null;
            IDrpQueueItem queueItem = null;
            int acquireAttempts = 0;
            int maxRetryAttempts = GetAcquireRetryAttempts();

            if (ValidateAquireId(acquirerId, "Acquire", queueItemId))
            {
                while (acquireAttempts < maxRetryAttempts && null == acquiredItem)
                {
                    acquireAttempts++;
                    acquiredItem = this.AttemptAcquire(acquirerId, queueItemId);
                }

                if (null != acquiredItem)
                {
                    queueItem = this.SetQueueItemAcquired(acquiredItem);
                }
            }

            if (null == queueItem)
            {
                this.WriteAppLogEntry(
                    "[Acquire].[Failure]",
                     string.Format(
                        "[Acquire: acquireId: {0} queueItemId: {1}]",
                        acquirerId,
                        queueItemId.ToStringOuterBraces()));
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
        /// <param name="acquirerId">Required - Must match database value - Case Sensitive</param>
        /// <param name="queueItemId">Id of queue item to release</param>
        /// <returns>Released Item on success, null on failure</returns>
        public IDrpQueueItem Release(string acquirerId, Guid queueItemId)
        {
            DrpServerQueueItem queueItem = null;
            DrpServerQueueAcquiredItem toRelease = null;
            DrpServerQueueEnqueuedItem toEnqueue = null;

            if (ValidateAquireId(acquirerId, "Release", queueItemId))
            {
                try
                {
                    toRelease = this.ServerQueueAcquiredItems.Where(
                        sqi => sqi.QueueItemId.Equals(queueItemId)
                        && acquirerId.Equals(sqi.AcquiredBy, StringComparison.Ordinal)
                        ).FirstOrDefault();

                    if (null != toRelease)
                    {
                        queueItem = this.ServerQueueItems.Find(queueItemId);
                    }
                }
                catch (System.Exception ex)
                {
                    this.WriteAppLogEntry(
                        "[Release].[Failure]",
                        string.Format(
                            "[Initial Select. acquireId: {0} queueItemId: {1}]",
                            acquirerId,
                            queueItemId.ToStringOuterBraces()),
                        ex);
                    Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.Release", ex);
                    toRelease = null;
                    queueItem = null;
                }

                // If the item to release was found
                if (null != queueItem)
                {
                    using (DbContextTransaction transaction = this.Database.BeginTransaction())
                    {
                        try
                        {
                            queueItem.Release();

                            // Create a new EnqueuedItem base on the AcquiredItem
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
                                "[Release].[Success]",
                                string.Format(
                                    "[Release: acquireId: {0} queueItemId: {1}]",
                                    acquirerId,
                                    queueItemId.ToStringOuterBraces()));
                        }
                        catch (System.Exception ex)
                        {
                            transaction.Rollback();
                            this.WriteAppLogEntry(
                                "[Release].[Failure]",
                                string.Format(
                                    "[RolledBack: acquireId: {0} queueItemId: {1}]",
                                    acquirerId,
                                    queueItemId.ToStringOuterBraces()),
                                ex);
                            Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.Release", ex);
                            queueItem = null;
                        }
                    }
                }
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
            DrpServerQueueItem queueItem = null;
            DrpServerQueueHistoryItem historyItem = null;
            DrpServerQueueDequeuedItem dequeuedItem = null;
            DrpServerQueueAcquiredItem acquiredItem = null;

            if (ValidateAquireId(acquirerId, "Dequeue", queueItemId))
            {
                try
                {
                    acquiredItem = this.ServerQueueAcquiredItems.Where(
                        sqi => sqi.QueueItemId.Equals(queueItemId)
                        && acquirerId.Equals(sqi.AcquiredBy, StringComparison.Ordinal)
                        ).FirstOrDefault();

                    if(null != acquiredItem)
                    {
                        queueItem = this.ServerQueueItems.Find(queueItemId);
                    }
                }
                catch (System.Exception ex)
                {
                    this.WriteAppLogEntry(
                        "[Dequeue].[Failure]",
                        string.Format(
                            "[Initial Select. acquireId: {0} queueItemId: {1}]",
                            acquirerId,
                            queueItemId.ToStringOuterBraces()),
                        ex);
                    Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.Dequeue", ex);
                    acquiredItem = null;
                    queueItem = null;
                }

                // If the item to Dequeue was found
                if (null != queueItem)
                {
                    using (DbContextTransaction transaction = this.Database.BeginTransaction())
                    {
                        try
                        {
                            dequeuedItem = new DrpServerQueueDequeuedItem(acquiredItem);
                            historyItem = new DrpServerQueueHistoryItem(queueItem);
                            // If the add failed, then the item was acquired after the original DrpServerQueryItem was retrieved.
                            this.ServerQueueDequeuedItems.Add(dequeuedItem);
                            this.ServerQueueAcquiredItems.Remove(acquiredItem);
                            this.ServerQueueHistoryItems.Add(historyItem);
                            this.ServerQueueItems.Remove(queueItem);
                            this.SaveChanges();
                            transaction.Commit();
                            this.WriteQueueLogEntry(
                                acquiredItem.Id,
                                dequeuedItem.Id,
                                queueItemId,
                                "[Dequeue].[Success]",
                                string.Format(
                                    "[Dequeue: acquireId: {0} queueItemId: {1}]",
                                    acquirerId,
                                    queueItemId.ToStringOuterBraces()));
                        }
                        catch (System.Exception ex)
                        {
                            transaction.Rollback();
                            this.WriteAppLogEntry(
                                "[Dequeue].[Failure]",
                                string.Format(
                                    "[RolledBack: acquireId: {0} queueItemId: {1}]",
                                    acquirerId,
                                    queueItemId.ToStringOuterBraces()),
                                ex);
                            Drp.DrpExceptionHandler.LogException("DrpServerQueueDataContext.Dequeue", ex);
                            queueItem = null;
                        }
                    }
                }
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
                        ret = this.ServerQueueAcquiredItems.Select(sqi => sqi.QueueItemId).ToList();
                    }
                    else
                    {
                        // Specific type values
                        ret = this.ServerQueueAcquiredItems.Where(
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
                        ret = this.ServerQueueAcquiredItems.Where(sqi => sqi.Acquired >= testDateTime)
                            .Select(sqi => sqi.QueueItemId).ToList();
                    }
                    else
                    {
                        // Specific ItemType value
                        ret = this.ServerQueueAcquiredItems.Where(
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
        public IDrpQueueItem RequeueAquiredItem(Guid queueItemId)
        {
            string acquirerId = string.Empty;
            DrpServerQueueItem queueItem = null;
            DrpServerQueueAcquiredItem toRelease = null;
            DrpServerQueueEnqueuedItem toEnqueue = null;

            try
            {
                toRelease = this.ServerQueueAcquiredItems.Where(
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
                    if (null != this.RequeueAquiredItem(queueitemId))
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
                    ret = this.ServerQueueEnqueuedItems.Where(sqi => sqi.Created >= testDateTime)
                        .Select(sqi => sqi.QueueItemId).ToList();
                }
                else
                {
                    // Specific ItemType value
                    ret = this.ServerQueueEnqueuedItems.Where(
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
