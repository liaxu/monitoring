%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe E:\SourceCode\AILogInsertDBService\AILogInsertDBService\bin\Debug\AILogInsertDBService.exe
Net Start AILogInsertDBService
sc config AILogInsertDBService start= auto
pause
