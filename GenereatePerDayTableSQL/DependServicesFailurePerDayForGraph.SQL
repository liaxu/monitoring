DECLARE @startTime DateTime = SUBSTRING(CONVERT(nvarchar(100),GetDate()-1,121),1,10) 
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
	DELETE FROM dbo.[DependServicesFailurePerDayForGraph] WHERE [CreateTime]=@dateTime 

	INSERT INTO dbo.[DependServicesFailurePerDayForGraph] 
	SELECT [DependServiceName],  
	@dateTime AS [CreateTime], 
	(CASE WHEN SUM(CASE WHEN [Alert]='Y' THEN 1 ELSE 0 END)>0 THEN 'Y' ELSE 'N' END) AS [Alert]	
	FROM dbo.[DependServicesFailurePerHourForGraph]
	WHERE [CreateTime]>=@dateTime AND [CreateTime]<(CONVERT(BIGINT,@startTime)*1000*60*60*24+59926608000000)
	GROUP BY [DependServiceName]	
END