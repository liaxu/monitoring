%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe E:\SourceCode\AILogDownloadService\AILogDownloadService\bin\Debug\AILogDownloadService.exe
Net Start AILogDownloadService
sc config AILogDownloadService start= auto
pause
