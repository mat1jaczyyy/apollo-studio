@echo off

cd ..\Apollo
rd /S /Q bin
rd /S /Q obj
dotnet clean
dotnet publish -r win-x64 -c Release
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Tools\MSVC\14.38.33130\bin\Hostx64\x64\editbin.exe" /subsystem:windows bin\Release\net5.0\win-x64\publish\Apollo.exe

echo.

cd ..\ApolloUpdate
rd /S /Q bin
rd /S /Q obj
dotnet clean
dotnet publish -r win-x64 -c Release
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Tools\MSVC\14.38.33130\bin\Hostx64\x64\editbin.exe" /subsystem:windows bin\Release\net5.0\win-x64\publish\ApolloUpdate.exe

echo.
echo Merging...

cd ..
rd /S /Q Build >nul 2>&1
mkdir Build
cd Build

mkdir Apollo
mkdir M4L
mkdir Update

robocopy ..\Apollo\bin\Release\net5.0\win-x64\publish Apollo /E >nul 2>&1
robocopy ..\ApolloUpdate\bin\Release\net5.0\win-x64\publish Update /E >nul 2>&1

robocopy ..\M4L M4L *.amxd >nul 2>&1

echo Creating Windows Installer...

cd ..
rd /S /Q Dist >nul 2>&1
mkdir Dist

"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /q Publish\Apollo.iss

echo Done.