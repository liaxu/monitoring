DECLARE @startTime DateTime = SUBSTRING(CONVERT(nvarchar(100),GetDate(),121),1,10) 
DECLARE @endTime BIGINT = CONVERT(BIGINT,GETDATE())*1000*60*60*24+59926608000000
DECLARE @dateTime BIGINT = CONVERT(BIGINT,@startTime)*1000*60*60*24+59926608000000

DELETE FROM dbo.[UVandPVPerDayForGraph] WHERE [DateTime]>=@dateTime

INSERT INTO dbo.[UVandPVPerDayForGraph]
SELECT [dayTime], COUNT(*), COUNT(DISTINCT [ip]), AVG([actionTime])	
FROM dbo.[UVandPVForGraph]
WHERE [dayTime]>=@dateTime AND ([Status]<300 or [Status]>=400)
GROUP BY [dayTime]
	
