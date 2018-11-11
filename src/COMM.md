# Communication Protocol for Apollo

## api

* /add_device:
  * Adds new instance of device at position in the chain.
  * request: json {track: int, index: int, device: string}
  * response: json device data
* /delete_device:
  * Removes device at position in the chain.
  * request: json {track: int, index: int}
  * response: null

## app

* Calls hosted on the app server will be documented here.
