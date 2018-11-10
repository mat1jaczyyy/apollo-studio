# Communication Protocol for Apollo

## api

* /add_device:
    * Adds new instance of device at position in the chain.
    * request: json {track: int, index: int, device: string}
    * response: json device data

## app

* Calls hosted on the app server will be documented here.
