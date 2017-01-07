Select Count(*) As AcquiredCount From ServerQueueAcquiredItem
Select Count(*) As EnqueuedCount From ServerQueueEnqueuedItem
Select Count(*) As ActiveItemsCount From ServerQueueItem
Select Count(*) As DequeuedCount From ServerQueueDequeuedItem
Select Count(*) As HistoryItemsCount From ServerQueueHistoryItem
Select Count(*) as LogEntryCount From ServerQueueLog