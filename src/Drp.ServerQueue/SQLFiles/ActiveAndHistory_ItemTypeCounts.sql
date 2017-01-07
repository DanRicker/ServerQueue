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

SELECT
	YEAR([LogDateTime]) AS 'YEAR',
	DATEPART(DAYOFYEAR, [LogDateTime]) AS 'DayOfYear',
	DATEPART(HH, [LogDateTime]) AS 'HourOfDay',
	MIN([LogDateTime]) AS 'StartTime',
	MAX([LogDateTime]) AS 'EndTime',
	Count(LogEntryId) AS 'LogEntryCount'
FROM dbo.ServerQueueLog
GROUP BY
	YEAR([LogDateTime]),
	DATEPART(DAYOFYEAR, [LogDateTime]),
	DATEPART(HH, [LogDateTime])
ORDER BY
	YEAR([LogDateTime]),
	DATEPART(DAYOFYEAR, [LogDateTime]),
	DATEPART(HH, [LogDateTime])
