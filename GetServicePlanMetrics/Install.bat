%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe E:\SourceCode\GetServicePlanMetrics\GetServicePlanMetrics\bin\Debug\GetServicePlanMetrics.exe
Net Start GetServicePlanMetrics
sc config GetServicePlanMetrics start= auto
pause
