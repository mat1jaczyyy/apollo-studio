# Communication Protocol for Apollo

## api (.NET Core)

* `/add_device`:
    * Adds new instance of device at position in the chain.
    * request: 
    ```js
        {
            "track": int,
            "index": int,
            "device": string
        }
    ```
    * response: 
    ```js
        {
            "device": string,
            "data": {
                // device-specific data
            }
        }
    ```
* `/delete_device`:
    * Removes device at position in the chain.
    * request: 
    ```js
        {
            "track": int,
            "index": int
        }
    ```
    * response: 
    ```js
        null
    ```

## app (Electron)

* `/init`:
    * .NET Core Host has initialized and returns Apollo Set information.
    * request:
    ```js
    {
        "object": "set", 
        "bpm": int, 
        "tracks": {
            "count": int,
            "i": { // i = 0 to (this.count - 1)
                "object": "track",
                "data": {
                    "chain": {
                        "object": "chain",
                        "data": {
                            "count": int,
                            "j": { // j = 0 to (this.count - 1)
                                "object": "device",
                                "data": {
                                    "device:" string,
                                    "data": {
                                        // device-specific data
                                    }
                                }
                            }
                        }
                    },
                    "launchpad": {
                        "object": "launchpad",
                        "data": {
                            "port": string
                        }
                    }
                }
            }
        }
    }
    ```
    * response: 
    ```js
        null
    ```
