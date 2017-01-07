/*

    Copyright 2016 Daniel Ricker III and Peoplutions

The core "Acquire Operations" made at the lowest system level possible to
1) Remove as much network latency as possible from the "transaction" processing.
2) Move Exception Processing to as close to the operation as possible.

Attempts to Dequeue an Acquired Item in a Transaction.
   - Delete Existing Acquired State Item
   - Add new Dequeued State Item
   - Delete Existing Queue Item
   - Add new QueueHistory Item

The "Delete" is the point of failure. An Acquired Item and an QueueItem can only be deleted once.
 This ensures a dequeue action.

DROP PROCEDURE [dbo].[spQueueItemDequeue];

GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

*/

CREATE PROCEDURE [dbo].[spQueueItemDequeue]
    @queueItemId UniqueIdentifier,
    @acquiredBy NVARCHAR(255) = NULL

AS

    SET NOCOUNT ON
    
    DECLARE @spReturnState INT = 1;

    DECLARE @acquireId UniqueIdentifier = null;
    DECLARE @itemType NVARCHAR(255) = null; 
    DECLARE @acquired DATETIME = null;
    DECLARE @created DATETIME = null;
    
    IF (@queueItemId IS NOT NULL)
    BEGIN

        IF (@acquiredBy IS NULL)
        BEGIN
           SET @acquiredBy = SUSER_NAME();
        END

        SET @spReturnState = @spReturnState + 1;

        -- Retrieve the Acquired item values
        SELECT
            @acquireId = Id,
            @itemType = ItemType,
			@acquired = Acquired,
            @created = Created
        FROM [dbo].[ServerQueueAcquiredItem]
        WHERE QueueItemId = @queueItemId AND AcquiredBy = @acquiredBy

		-- If the Acquired Item was not retrieved then it was already Deleted (relased or dequeued).
        if (@acquireId IS NOT NULL)
        BEGIN

			SET @spReturnState = @spReturnState + 1;

			DECLARE @itemId NVARCHAR(255) = NULL;
			DECLARE @itemData NVARCHAR(MAX) = NULL;
			DECLARE @itemMetadata NVARCHAR(MAX) = NULL;

			SELECT
			    @itemId = ItemId,
				@itemData = ItemData,
				@itemMetadata = ItemMetadata
			FROM [dbo].[ServerQueueItem]
			WHERE Id = @queueItemId


            BEGIN TRANSACTION

	        SET @spReturnState = @spReturnState + 1;

            BEGIN TRY
				-- Core Dequeue Operations as Transaction

				-- Delete the Acquired Entry - If this fails, then Enqueue Item was already acquired
                DELETE FROM [dbo].[ServerQueueAcquiredItem] WHERE Id = @acquireId;
				-- Delete the Active Queue Entry
				DELETE FROM [dbo].[ServerQueueItem] WHERE Id = @queueItemId;

				DECLARE @dequeueId UniqueIdentifier = NEWID();
				DECLARE @historyCreated DATETIME = SYSUTCDATETIME();

				-- Add the Acquired Entry
                INSERT INTO [dbo].[ServerQueueDequeuedItem] (
					Id,
					Acquired,
					AcquiredBy,
					QueueItemId,
					ItemType,
					Created,
					HistoryCreated)
                VALUES (
					@dequeueId,
					@acquired,
					@acquiredBy,
					@queueItemId,
					@itemType,
					@created,
					@historyCreated);

				INSERT INTO [dbo].[ServerQueueHistoryItem] (
					Id,
					HistoryCreated,
					Created,
					ItemType,
					ItemId,
					ItemData,
					ItemMetadata,
					AcquiredBy,
					Acquired)
				VALUES (
					@queueItemId,
					@historyCreated,
					@created,
					@itemType,
					@itemId,
					@itemData,
					@itemMetadata,
					@acquiredBy,
					@acquired)

                COMMIT TRANSACTION;
                SET @spReturnState = 0;

            END TRY

            BEGIN CATCH
                ROLLBACK TRANSACTION;
                SET @spReturnState = -1;
            END CATCH

        END

    END

    SET NOCOUNT OFF;

	-- Return the Dequeued Item Entry IF it has the expected Id value
	-- DO NOT RETURN "WHERE QueueItemId = @queueItemId"
	--		if some other process already Dequeued then this should return a failure (empty record set)
	--		The process that succeeded should take subsequent actions not this process.
    SELECT * FROM [dbo].[ServerQueueDequeuedItem] WHERE Id = @dequeueId;

	RETURN @spReturnState
