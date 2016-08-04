%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe E:\SourceCode\PageViewsLogDownloadService\PageViewsLogDownloadService\bin\Debug\PageViewsLogDownloadService.exe
Net Start PageViewsLogDownloadService
sc config PageViewsLogDownloadService start= auto
pause
