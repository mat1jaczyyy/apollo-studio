@echo off

cd ..\Apollo
rd /S /Q bin
rd /S /Q obj
dotnet clean
dotnet publish -r win-x64 -c Release
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Tools\MSVC\14.25.28610\bin\Hostx64\x64\editbin.exe" /subsystem:windows bin\Release\netcoreapp3.1\win-x64\publish\Apollo.exe >nul 2>&1

echo.

cd ..\ApolloUpdate
rd /S /Q bin
rd /S /Q obj
dotnet clean
dotnet publish -r win-x64 -c Release
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Tools\MSVC\14.25.28610\bin\Hostx64\x64\editbin.exe" /subsystem:windows bin\Release\netcoreapp3.1\win-x64\publish\ApolloUpdate.exe >nul 2>&1

echo.
echo Merging...

cd ..
rd /S /Q Build >nul 2>&1
mkdir Build
cd Build

mkdir Apollo
mkdir M4L
mkdir Update

robocopy ..\Apollo\bin\Release\netcoreapp3.1\win-x64\publish Apollo /E >nul 2>&1
robocopy ..\Apollo Apollo elevate.exe >nul 2>&1

robocopy ..\ApolloUpdate\bin\Release\netcoreapp3.1\win-x64\publish Update /E >nul 2>&1
robocopy ..\ApolloUpdate Update handle64.exe >nul 2>&1

robocopy ..\M4L M4L *.amxd >nul 2>&1

echo Creating Windows Installer...

cd ..
rd /S /Q Dist >nul 2>&1
mkdir Dist

"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /q Publish\Apollo.iss

echo Done.