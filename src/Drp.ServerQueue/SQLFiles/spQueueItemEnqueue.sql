/*

    Copyright 2016 Daniel Ricker III and Peoplutions

Enqueue and new Item in a transaction
		1) Insert into ServerQueueItem new QueueItem
		2) Insert into ServerQueueEnqueuedItem new QueueStateItem

DROP PROCEDURE [dbo].[spQueueItemEnqueue];

GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

*/

CREATE PROCEDURE [dbo].[spQueueItemEnqueue]
    @itemId NVARCHAR(255) = NULL,
	@itemType NVARCHAR(255) = NULL,
    @itemData NVARCHAR(MAX),
    @itemMetadata NVARCHAR(MAX)
AS

    SET NOCOUNT ON
    
    DECLARE @spReturnState INT = 1;
	DECLARE @queueItemId UniqueIdentifier = NEWID();
    DECLARE @enqueueId UniqueIdentifier = NEWID();
    DECLARE @created DATETIME = SYSUTCDATETIME();
    
	IF (@itemData IS NOT NULL)
    BEGIN

        BEGIN TRANSACTION
			-- Insert the Queue Item
            BEGIN TRY
				INSERT INTO [dbo].[ServerQueueItem](
					Id,
					ItemType,
					Created,
					ItemId,
					ItemData,
					ItemMetadata,
					AcquiredBy,
					Acquired)
				VALUES(
					@queueItemId,
					@itemType,
					@created,
					@itemId,
					@itemData,
					@itemMetadata,
					NULL,
					NULL);

				-- Insert the Enqueued State Item
				INSERT INTO  [dbo].[ServerQueueEnqueuedItem] (
				    Id,
					QueueItemId,
					ItemType,
					Created
				)
				VALUES (
					@enqueueId,
					@queueItemId,
					@itemType,
					@created
				);

                COMMIT TRANSACTION;
                SET @spReturnState = 0;

            END TRY

            BEGIN CATCH
                ROLLBACK TRANSACTION;
                SET @spReturnState = -1;
            END CATCH

    END

    SET NOCOUNT OFF;

	-- Return the Enqueued Item - Returns empty record set on failure
    SELECT * FROM [dbo].[ServerQueueEnqueuedItem] WHERE Id = @enqueueId;

	RETURN @spReturnState
