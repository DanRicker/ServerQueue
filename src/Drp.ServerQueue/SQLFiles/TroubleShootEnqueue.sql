USE [DrpServerQueue0003]

SELECT
      history.HistoryCreated,
	  acquired.Id,
	  acquired.QueueItemId,
	  history.ItemType,
	  acquired.Acquired,
	  acquired.AcquiredBy,
	  history.Created 
FROM
    [dbo].[ServerQueueAcquiredItem] acquired,
	[dbo].[ServerQueueHistoryItem] history
WHERE
   acquired.QueueItemId = history.Id
   AND
   history.HistoryCreated < DATEADD(n, -1, SYSUTCDATETIME())
ORDER BY history.HistoryCreated DESC

SELECT Count(*)
FROM
    [dbo].[ServerQueueAcquiredItem] acquired,
	[dbo].[ServerQueueItem] active
WHERE
   acquired.QueueItemId = active.Id


   SELECT DATEADD(n, -5, SYSUTCDATETIME()), SYSUTCDATETIME(), (SYSUTCDATETIME() > DATEADD(n, -5, SYSUTCDATETIME())) As GREATER


SELECT * 
FROM
    [dbo].[ServerQueueAcquiredItem] acquired,
	[dbo].[ServerQueueDequeuedItem] dequeued
WHERE
   acquired.QueueItemId = dequeued.QueueItemId

SELECT TOP 10 *
FROM [dbo].[ServerQueueAcquiredItem]

SELECT * 
FROM [dbo].[ServerQueueAcquiredItem]
WHERE QueueItemId = 'df4e3fe3-aa8a-420a-b34c-0cac92a5cd5e'



SELECT SYSUTCDATETIME() AS CurrentDateTimeUtc

SELECT * FROM [dbo].[ServerQueueLog]
WHERE LogEntry LIKE '%3f4439b6-6276-4a71-9c3a-26c598a04d8a%'

SELECT *
FROM [dbo].[ServerQueueAcquiredItem]
WHERE AcquiredBy LIKE '%3f4439b6-6276-4a71-9c3a-26c598a04d8a%'