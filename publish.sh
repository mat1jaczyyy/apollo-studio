cd Apollo
dotnet publish -r osx.10.11-x64 -c Release
sips -i icon.ico > /dev/null
DeRez -only icns icon.ico > tmpicns.rsrc
Rez -append tmpicns.rsrc -o bin/Release/netcoreapp2.2/osx.10.11-x64/publish/Apollo > /dev/null
SetFile -a C bin/Release/netcoreapp2.2/osx.10.11-x64/publish/Apollo > /dev/null
rm tmpicns.rsrc > /dev/null
rm bin/Release/netcoreapp2.2/win10-x64/publish/Apollo.config > /dev/null 2>&1

echo

cd ../ApolloUpdate
dotnet publish -r osx.10.11-x64 -c Release
sips -i icon.ico > /dev/null
DeRez -only icns icon.ico > tmpicns.rsrc
Rez -append tmpicns.rsrc -o bin/Release/netcoreapp2.2/osx.10.11-x64/publish/ApolloUpdate >/dev/null
SetFile -a C bin/Release/netcoreapp2.2/osx.10.11-x64/publish/ApolloUpdate >/dev/null
rm tmpicns.rsrc > /dev/null

echo
echo Merging...

cd ..
rm -rf Build
mkdir Build
cd Build

mkdir Apollo
mkdir M4L
mkdir Update

cp -r ../Apollo/bin/Release/netcoreapp2.2/osx.10.11-x64/publish/* Apollo
cp "../M4L/Apollo Connector.amxd" M4L
cp -r ../ApolloUpdate/bin/Release/netcoreapp2.2/osx.10.11-x64/publish/* Update

echo Done.