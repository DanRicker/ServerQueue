Select 
	ItemType as ActiveItemType,
	Count(id) as ItemTypeCount
From ServerQueueItem
GROUP BY ItemType


Select 
	ItemType as HistoryItemType,
	Count(id) as ItemTypeCount
From ServerQueueHistoryItem
GROUP BY ItemType