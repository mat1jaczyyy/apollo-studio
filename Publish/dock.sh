#!/bin/sh

chmod +x "$2/Apollo/Apollo"
chmod +x "$2/Update/ApolloUpdate"

if [ "$COMMAND_LINE_INSTALL" = "" ]; then

	mv "$2/Apollo/Apollo" "$2/Apollo/Apollo.app"
	/usr/bin/su $USER -c "./emmett add \"$2/Apollo/Apollo.app\""
	mv "$2/Apollo/Apollo.app" "$2/Apollo/Apollo"

fi

exit 0