@echo off

cd ..\Apollo
rd /S /Q bin
rd /S /Q obj
dotnet clean
dotnet publish --self-contained true -r win-x64 -c Release -p:OutputType=WinExe

echo.

cd ..\ApolloUpdate
rd /S /Q bin
rd /S /Q obj
dotnet clean
dotnet publish --self-contained true -r win-x64 -c Release -p:OutputType=WinExe

echo.
echo Merging...

cd ..
rd /S /Q Build >nul 2>&1
mkdir Build
cd Build

mkdir Apollo
mkdir M4L
mkdir Update

robocopy ..\Apollo\bin\Release\net10.0\win-x64\publish Apollo /E >nul 2>&1
robocopy ..\ApolloUpdate\bin\Release\net10.0\win-x64\publish Update /E >nul 2>&1

robocopy ..\M4L M4L *.amxd >nul 2>&1

echo Creating Windows Installer...

cd ..
rd /S /Q Dist >nul 2>&1
mkdir Dist

"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /q Publish\Apollo.iss

echo Done.