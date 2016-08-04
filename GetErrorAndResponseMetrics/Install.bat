%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe E:\SourceCode\GetErrorAndResponseMetrics\GetErrorAndResponseMetrics\bin\Debug\GetErrorAndResponseMetrics.exe
Net Start GetErrorAndResponseMetrics
sc config GetErrorAndResponseMetrics start= auto
pause
