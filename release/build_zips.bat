@echo off

if "%1"=="" goto usage
if "%2"=="" goto usage
if "%3"=="" goto usage

set SEVENZ="C:\Program Files\7-Zip\7z.exe"
SET VERSIONIZE="..\..\release\autoversionator\AutoVersionator.exe"

cd ..\src\NoFrillsTransformation

%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Interfaces\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Logging\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Operators\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Operators.Utils\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Ado\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Ado.MySql\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Ado.Oracle\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Ado.Sqlite\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Ado.SqlServer\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTranformation.Plugins.CsvReader\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Inline\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Salesforce\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Salesforce.Test\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Sap\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Statistics\Properties\AssemblyInfo.cs
if errorlevel 1 goto error
%VERSIONIZE% %1 %2 %3 0 NoFrillsTransformation.Plugins.Xml\Properties\AssemblyInfo.cs
if errorlevel 1 goto error

devenv.exe NoFrillsTransformation.sln /Rebuild "Release|x64"
if errorlevel 1 goto error

devenv.exe NoFrillsTransformation.sln /Rebuild "Release|x86"
if errorlevel 1 goto error

cd ..\..\bin\x64\Release

set RELTIME=%date:~6,4%%date:~3,2%%date:~0,2%%time:~0,2%%time:~3,2%
if "%time:~0,1%"==" " (
	set RELTIME=%date:~6,4%%date:~3,2%%date:~0,2%0%time:~1,1%%time:~3,2%
)
set RELNAME=%1_%2_%3_%RELTIME%

%SEVENZ% a ..\..\..\release\nft_x64_v%RELNAME%.zip @..\..\..\release\x64.txt -x!NoFrillsTransformation.Plugins.Sap*.dll
if errorlevel 1 goto zipError
%SEVENZ% a ..\..\..\release\nft_x64_SAP_v%RELNAME%.zip NoFrillsTransformation.Plugins.Sap*.dll
if errorlevel 1 goto zipError

cd ..\..\x86\Release

%SEVENZ% a ..\..\..\release\nft_x86_v%RELNAME%.zip @..\..\..\release\x86.txt
if errorlevel 1 goto zipError

cd ..\..\..\release

goto done

:usage

echo.
echo Usage:
echo   build_zips.bat ^<major^> ^<minor^> ^<build^>
echo.

goto done

:zipError

echo.
echo There were ZIP errors, dude.
echo.

cd ..\..\..\release

goto done

:error

echo.
echo There were errors, dude.
echo.

cd ..\..\release

goto reallyDone

:done

copy /y nft_x64_v%RELNAME%.zip nft_x64_latest.zip
copy /y nft_x64_SAP_v%RELNAME%.zip nft_x64_SAP_latest.zip
copy /y nft_x86_v%RELNAME%.zip nft_x86_latest.zip

:reallyDone