DECLARE @startTime DateTime = SUBSTRING(CONVERT(nvarchar(100),GetDate(),121),1,10) 
DECLARE @endTime DateTime = GETDATE()
DECLARE @FailedCount INT
DECLARE @PassedCount INT
DECLARE @responseTime FLOAT
DECLARE @dateTime BIGINT
DECLARE @webTestName NVARCHAR(MAX)
DECLARE @applicationName NVARCHAR(MAX)
DECLARE @location NVARCHAR(50)
		
WHILE(@startTime<=@endTime)
BEGIN
	SET @FailedCount=0 
	SET @PassedCount=0
	SET @responseTime=0
	SET @dateTime = CONVERT(BIGINT,@startTime)*1000*60*60*24+59926608000000
	SET @startTime = DATEADD(DAY,1,@startTime)
	SET @webTestName = NULL
	SET @applicationName = NULL
	SET @location = NULL
	DELETE FROM dbo.[ResponsePerDayForGraph] WHERE [DateTime]=@dateTime 

	INSERT INTO dbo.[ResponsePerDayForGraph]
	SELECT @dateTime AS [DateTime], 
	AVG([ResponseTime(s)]) AS [ResponseTime(s)], 
	SUM([FailedCount]) AS [FailedCount], 
	SUM([PassedCount]) AS [PassedCount],  
	[WebTestName], [ApplicationName], [Location]	
	FROM dbo.[ResponsePerHourForGraph]
	WHERE [DateTime]>=@dateTime AND [DateTime]<(CONVERT(BIGINT,@startTime)*1000*60*60*24+59926608000000)
	GROUP BY [WebTestName], [ApplicationName], [Location]	
END