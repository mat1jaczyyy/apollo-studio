#!/bin/sh

cd ../Apollo
rm -rf bin
rm -rf obj
dotnet clean
dotnet publish -r osx-x64 -c Release
fileicon set bin/Release/net5.0/osx-x64/publish/Apollo icon.ico

echo

cd ../ApolloUpdate
rm -rf bin
rm -rf obj
dotnet clean
dotnet publish -r osx-x64 -c Release
fileicon set bin/Release/net5.0/osx-x64/publish/ApolloUpdate icon.ico

echo
echo Merging...

cd ..
rm -rf Build
mkdir Build
cd Build

mkdir Apollo
mkdir M4L
mkdir Update

cp -r ../Apollo/bin/Release/net5.0/osx-x64/publish/* Apollo
cp -r ../ApolloUpdate/bin/Release/net5.0/osx-x64/publish/* Update

cp ../M4L/*.amxd M4L

echo Creating macOS Package...

cd ..
rm -rf Dist
mkdir Dist

packagesbuild Publish/Apollo.pkgproj

echo Done.