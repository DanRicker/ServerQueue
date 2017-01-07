
/*

    Copyright 2016 Daniel Ricker III and Peoplutions

The core "Release Operations" made at the lowest system level possible to
1) Remove as much network latency as possible from the "transaction" processing.
2) Move Exception Processing to as close to the operation as possible.

Attempts to "Release" an Acquired Item in a Transaction.
   - Delete Existing Acquired Item
   - Add new Enqueued Item

The "Delete" is the point of failure. An Acquired Item can only be deleted once.
 This ensures a single Release success.

DROP PROCEDURE [dbo].[spQueueItemRelease];

GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

*/

CREATE PROCEDURE [dbo].[spQueueItemRelease]
    @queueItemId UniqueIdentifier,
    @acquiredBy NVARCHAR(255) = NULL

AS

    SET NOCOUNT ON
    
    DECLARE @spReturnState INT = 1;

	DECLARE @acquireId UniqueIdentifier = null;
    DECLARE @itemType NVARCHAR(255) = null; 
    
    IF (@queueItemId IS NOT NULL)
    BEGIN

        IF (@acquiredBy IS NULL)
        BEGIN
           SET @acquiredBy = SUSER_NAME();
        END

        SET @spReturnState = @spReturnState + 1;

        -- Retrieve the Enqueued item values
        SELECT
            @acquireId = Id,
            @itemType = ItemType
        FROM [dbo].[ServerQueueAcquiredItem]
        WHERE QueueItemId = @queueItemId AND AcquiredBy = @acquiredBy

		-- If the Enqueued Item was not retrieved then it was already aquired.
        if (@acquireId IS NOT NULL)
        BEGIN

			DECLARE @enqueueId UniqueIdentifier = NEWID();
			DECLARE @created DATETIME = SYSUTCDATETIME();

			SET @spReturnState = @spReturnState + 1;

            BEGIN TRANSACTION

            BEGIN TRY
				-- Core Release Operations as Transaction

				-- Delete the Enqueued Entry - If this fails, then Acquired Item was already deleted (Dequeued or Release)
                DELETE FROM [dbo].[ServerQueueAcquiredItem] WHERE Id = @acquireId;

				-- Add the Enqueue Entry
                INSERT INTO [dbo].[ServerQueueEnqueuedItem] (
					Id,
					QueueItemId,
					ItemType,
					Created)
                VALUES (
					@enqueueId,
					@queueItemId,
					@itemType,
					@created);

				-- Update ServerQueueItem with NULLs (not acquired)
				UPDATE [dbo].[ServerQueueItem]
					SET
						AcquiredBy = NULL,
						Acquired = NULL
				 WHERE [dbo].[ServerQueueItem].ID = @queueItemId

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

	-- Return the enqueued Item Entry IF it has the expected Id value
	-- DO NOT RETURN "WHERE QueueItemId = @queueItemId"
	--		if some other process already released then this should return a failure
	--		The process that succeeded should take subsequent actions not this process.
    SELECT * FROM [dbo].[ServerQueueEnqueuedItem] WHERE Id = @enqueueId;

	RETURN @spReturnState
