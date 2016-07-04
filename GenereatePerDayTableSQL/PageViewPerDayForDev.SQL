DECLARE @startTime DateTime = SUBSTRING(CONVERT(nvarchar(100),GetDate(),121),1,10) 
DECLARE @endTime BIGINT = CONVERT(BIGINT,GETDATE())*1000*60*60*24+59926608000000
DECLARE @dateTime BIGINT = CONVERT(BIGINT,@startTime)*1000*60*60*24+59926608000000

DELETE FROM dbo.[PageViewPerDayForDev] WHERE [DateTime]>=@dateTime

INSERT INTO dbo.[PageViewPerDayForDev]
SELECT [dayTime] AS [DateTime], 
	[URL], COUNT(*) AS [PV], 
	SUM(CASE WHEN [status]>=100 and [status]<200 THEN 1 ELSE 0 END) AS [PV-1XX],
	SUM(CASE WHEN [status]>=200 and [status]<300 THEN 1 ELSE 0 END) AS [PV-2XX],
	SUM(CASE WHEN [status]>=400 and [status]<500 THEN 1 ELSE 0 END) AS [PV-4XX],
	SUM(CASE WHEN [status]>=500 and [status]<600 THEN 1 ELSE 0 END) AS [PV-5XX],
	COUNT(DISTINCT [ip]) AS [UV], 
	AVG([actionTime]) AS [ResponseTime]
FROM dbo.[UVandPVForDev]	
WHERE [dayTime]>=@dateTime AND ([Status]<300 or [Status]>=400)
GROUP BY [dayTime], [URL]