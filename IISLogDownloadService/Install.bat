%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe E:\SourceCode\IISLogDownloadService\IISLogDownloadService\bin\Debug\IISLogDownloadService.exe
Net Start IISLogDownloadService
sc config IISLogDownloadService start= auto
pause
