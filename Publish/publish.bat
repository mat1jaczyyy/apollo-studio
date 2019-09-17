@echo off

cd ..\Apollo
rd /S /Q bin\Release\netcoreapp2.2\win10-x64\publish
dotnet publish -r win10-x64 -c Release
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Tools\MSVC\14.22.27905\bin\Hostx64\x64\editbin.exe" /subsystem:windows bin\Release\netcoreapp2.2\win10-x64\publish\Apollo.exe >nul 2>&1
"C:\Users\mat1jaczyyy\Downloads\rcedit-x64.exe" --set-icon icon.ico bin\Release\netcoreapp2.2\win10-x64\publish\Apollo.exe
del /s bin\Release\netcoreapp2.2\win10-x64\publish\Apollo.config >nul 2>&1

echo.

cd ..\ApolloUpdate
rd /S /Q bin\Release\netcoreapp2.2\win10-x64\publish
dotnet publish -r win10-x64 -c Release
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Tools\MSVC\14.22.27905\bin\Hostx64\x64\editbin.exe" /subsystem:windows bin\Release\netcoreapp2.2\win10-x64\publish\ApolloUpdate.exe >nul 2>&1
"C:\Users\mat1jaczyyy\Downloads\rcedit-x64.exe" --set-icon icon.ico bin\Release\netcoreapp2.2\win10-x64\publish\ApolloUpdate.exe

echo.
echo Merging...

cd ..
rd /S /Q Build >nul 2>&1
mkdir Build
cd Build

mkdir Apollo
mkdir M4L
mkdir Update

robocopy ..\Apollo\bin\Release\netcoreapp2.2\win10-x64\publish Apollo /E >nul 2>&1
robocopy ..\Apollo Apollo elevate.exe >nul 2>&1

robocopy ..\ApolloUpdate\bin\Release\netcoreapp2.2\win10-x64\publish Update /E >nul 2>&1
robocopy ..\ApolloUpdate Update handle64.exe >nul 2>&1

robocopy ..\M4L M4L *.amxd >nul 2>&1

echo Creating Windows Installer...

cd ..
rd /S /Q Dist >nul 2>&1
mkdir Dist

"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /q Publish\Apollo.iss

echo Done.