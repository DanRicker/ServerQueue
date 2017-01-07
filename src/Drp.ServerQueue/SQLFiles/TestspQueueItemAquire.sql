

USE [DrpServerQueueExample00001]
GO
/* 
DECLARE	@return_value Int

EXEC	@return_value = [dbo].[spQueueItemAquire]
		@queueItemId = '69efca05-9826-44fe-91a3-8fc7cd4a806f',
		@acquireId = '182eb011-7684-4ba5-8695-7087022b8892',
		@acquiredBy = N'DanRicker'

SELECT	@return_value as 'Return Value'

GO

*/

/*

SELECT * FROM  [dbo].[ServerQueueAcquiredItem] WHERE QueueItemId = 'e61a953e-c699-479f-85ef-0546c3839348'

*/



    SET NOCOUNT ON;
    
    DECLARE @queueItemId UniqueIdentifier =  CONVERT(uniqueidentifier, 'd88548b8-61b7-4e7b-aeda-845273a6fb48');
    DECLARE @acquireId UniqueIdentifier =  CONVERT(uniqueidentifier, 'c0b69f2a-ba10-48cb-b49b-b9be57299b71');
    DECLARE @acquired DATE = NULL;
    DECLARE @acquiredBy VARCHAR(127) = null;


    DECLARE @spReturnState INT = 1;

    DECLARE @enqueueId UniqueIdentifier = null;
    DECLARE @itemType VARCHAR(127) = null; 
    DECLARE @created DATE = null;
    
    IF (@queueItemId IS NOT NULL)
    BEGIN

        IF (@acquired IS NULL)
        BEGIN
            SET @acquired = SYSUTCDATETIME();
        END

        IF (@acquiredBy IS NULL)
        BEGIN
           SET @acquiredBy = SUSER_NAME();
        END

        SET @spReturnState = 2

        -- Retrieve the Enqueued item values
        SELECT
            @enqueueId = Id,
            @itemType = ItemType,
            @created = Created
        FROM [dbo].[ServerQueueEnqueuedItem]
        WHERE QueueItemId = @queueItemId

        if (@enqueueId IS NOT NULL)
        BEGIN

        SET @spReturnState = 3;

            BEGIN TRANSACTION

            BEGIN TRY

                INSERT INTO [dbo].[ServerQueueAcquiredItem] (Id, Acquired, AcquiredBy, QueueItemId, ItemType, Created)
                VALUES(@acquireId, @acquired, @acquiredBy, @queueItemId, @itemType, @created);

                DELETE FROM [dbo].[ServerQueueEnqueuedItem] WHERE QueueItemId = @queueItemId;

                COMMIT TRANSACTION;
                SET @spReturnState = 0;

            END TRY

            BEGIN CATCH

              SELECT 
                  ERROR_NUMBER() AS ErrorNumber,
                  ERROR_SEVERITY() AS ErrorSeverity,
                  ERROR_STATE() AS ErrorState,
                  ERROR_PROCEDURE() AS ErrorProcedure,
                  ERROR_LINE() AS ErrorLine,
                  ERROR_MESSAGE() AS ErrorMessage

                ROLLBACK TRANSACTION;
                SET @spReturnState = 4;
            END CATCH

        END

    END

    SET NOCOUNT OFF;

    SELECT * FROM [dbo].[ServerQueueAcquiredItem] WHERE Id = @acquireId



