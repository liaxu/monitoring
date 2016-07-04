DECLARE @startTime DateTime = SUBSTRING(CONVERT(nvarchar(100),GetDate(),121),1,10) 
DECLARE @endTime DateTime = GETDATE()
DECLARE @PV INT
DECLARE @UV INT
DECLARE @responseTime FLOAT
DECLARE @dateTime BIGINT
		
WHILE(@startTime<=@endTime)
BEGIN
	SET @PV=0 
	SET @UV=0
	SET @responseTime=0
	SET @dateTime = CONVERT(BIGINT,@startTime)*1000*60*60*24+59926608000000
	SET @startTime = DATEADD(DAY,1,@startTime)

	DELETE FROM dbo.[LocationViewPerDayForGraph] WHERE [DateTime]=@dateTime 

	INSERT INTO dbo.[LocationViewPerDayForGraph]
	SELECT @dateTime AS [DateTime], [IP], COUNT(*) AS [PV], COUNT(DISTINCT [ip]) AS [UV], AVG([actionTime]) AS [ResponseTime]
	FROM dbo.[UVandPVForGraph]
	WHERE [dayTime]>=@dateTime AND [dayTime]<(CONVERT(BIGINT,@startTime)*1000*60*60*24+59926608000000) AND ([status]<300 or [status]>=400)
	GROUP BY [ip]
END