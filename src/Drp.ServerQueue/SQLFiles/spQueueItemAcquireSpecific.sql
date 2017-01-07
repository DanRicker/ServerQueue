
/*

    Copyright 2016 Daniel Ricker III and Peoplutions

The core "Acquire Operations" made at the lowest system level possible to
1) Remove as much network latency as possible from the "transaction" processing.
2) Move Exception Processing to as close to the operation as possible.

Attempts to "Acquire" an Enqueued Item in a Transaction.
   - Delete Existing Enqueued Item
   - Add new Acquired Item
   - Update QueueItem with Acquire information

The "Delete" is the point of failure. An Enqueued Item can only be deleted once.
 This ensures a single acquire success within the limits of SQL Server Capabilities.

DROP PROCEDURE [dbo].[spQueueItemAcquireQueueItem];

GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

*/


CREATE PROCEDURE [dbo].[spQueueItemAcquireSpecific]
	@queueItemId UniqueIdentifier = NULL,
    @acquiredBy NVARCHAR(255) = NULL

AS

    SET NOCOUNT ON
    
	-- Initialize to "entered"
    DECLARE @spReturnState INT = 1;
	DECLARE @itemType NVARCHAR(255);
    DECLARE @enqueueId UniqueIdentifier = null;
    DECLARE @created DATETIME = null;
    
    BEGIN
		-- Retrieve The specific Enqueue Item for the requested QueueItem
		SELECT
			@enqueueId = Id,
			@itemType = ItemType,
			@created = Created
		FROM [dbo].[ServerQueueEnqueuedItem]
		WHERE QueueItemId = @queueItemId

		-- Enqueued Item was found.
		if (@enqueueId IS NOT NULL)
		BEGIN

			DECLARE @acquireId UniqueIdentifier = NEWID();
			DECLARE @acquired DATETIME = SYSUTCDATETIME();

			IF (@acquiredBy IS NULL)
			BEGIN
				SET @acquiredBy = SUSER_NAME();
			END

			-- Increment for state information
			SET @spReturnState = @spReturnState + 1;

			BEGIN TRANSACTION

			BEGIN TRY
				-- Core Acquire Operations as Transaction

				-- Delete the Enqueued Entry - If this fails, then Enqueue Item was already acquired
				DELETE FROM [dbo].[ServerQueueEnqueuedItem] WHERE Id = @enqueueId;

				-- Add the Acquired Entry
				INSERT INTO [dbo].[ServerQueueAcquiredItem] (
					Id,
					Acquired,
					AcquiredBy,
					QueueItemId,
					ItemType,
					Created)
				VALUES(
					@acquireId,
					@acquired,
					@acquiredBy,
					@queueItemId,
					@itemType,
					@created);

				-- Update ServerQueueItem with Acquired
				UPDATE [dbo].[ServerQueueItem]
					SET
						AcquiredBy = @acquiredBy,
						Acquired = @acquired
					WHERE [dbo].[ServerQueueItem].ID = @queueItemId

				COMMIT TRANSACTION;
				-- Set to Success
				SET @spReturnState = 0;

			END TRY

			BEGIN CATCH
				-- Failures are expected. This is the point of convergence for a multiple acquire attempts
				-- Some other process probably acquired the item before this completed
				ROLLBACK TRANSACTION;
				-- Set to Error
				SET @spReturnState = -1;
			END CATCH

		END

    END

    SET NOCOUNT OFF;

	-- If Acquire failed (Rolled back), then this returns an empty data set.
    SELECT * FROM [dbo].[ServerQueueAcquiredItem] WHERE Id = @acquireId;

	RETURN @spReturnState
