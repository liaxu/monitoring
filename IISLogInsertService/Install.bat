%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe E:\SourceCode\IISLogInsert\IISLogInsertService\IISLogInsertService\bin\Debug\IISLogInsertService.exe
Net Start IISLogInsertService
sc config IISLogInsertService start= auto
pause
