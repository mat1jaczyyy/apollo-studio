@echo off

cd Apollo
dotnet publish -r win10-x64 -c Release
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.16.27023\bin\Hostx64\x64\editbin.exe" /subsystem:windows bin\Release\netcoreapp2.2\win10-x64\publish\Apollo.exe >nul 2>&1
"C:\Users\mat1jaczyyy\Downloads\rcedit-x64.exe" --set-icon icon.ico bin\Release\netcoreapp2.2\win10-x64\publish\Apollo.exe
del /s bin\Release\netcoreapp2.2\win10-x64\publish\Apollo.config >nul 2>&1

echo.

cd ..
cd ApolloUpdate
dotnet publish -r win10-x64 -c Release
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.16.27023\bin\Hostx64\x64\editbin.exe" /subsystem:windows bin\Release\netcoreapp2.2\win10-x64\publish\ApolloUpdate.exe >nul 2>&1
"C:\Users\mat1jaczyyy\Downloads\rcedit-x64.exe" --set-icon icon.ico bin\Release\netcoreapp2.2\win10-x64\publish\ApolloUpdate.exe

echo.
echo Merging...

cd ..
rd /S /Q Build
mkdir Build
cd Build

mkdir Apollo
mkdir M4L
mkdir Update

robocopy ..\Apollo\bin\Release\netcoreapp2.2\win10-x64\publish Apollo /E >nul 2>&1
robocopy ..\M4L M4L "Apollo Connector.amxd" >nul 2>&1
robocopy ..\ApolloUpdate\bin\Release\netcoreapp2.2\win10-x64\publish Update /E >nul 2>&1

echo Done.