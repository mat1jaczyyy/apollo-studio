# Communication Protocol for Apollo

## api (.NET Core)

All requests towards the api should target `localhost:1548/api`.

Generally, a message should be formatted as follows:

```js
    {
        "object": "message",
        "recipient": string, // Recipient object identifier
        "data": {
            "type": string, // Recipient-specific message type
            // Additional data (recipient-specific)
        }
    }
```

If the message contains another message that should be forwarded to one of the recipient's members, the message should look like this:

```js
    {
        "object": "message",
        "recipient": string, // Recipient object identifier
        "data": {
            "type": "forward", // Special forward message type
            "forward": string, // Forwardee object identifier
            "index": int, // If applicable, include an index for array-based members
            "message": {
                "object": "message",
                // ...
            }
        }
    }
```

The top-most recipient will ALWAYS be the currently loaded Set. You can nest multiple forward messages into each other.

### `set` object

The Set object contains the List of Tracks. It also does file management and stores the BPM value. It is the top-most object in the Apollo hierarchy.

* `new`:
    * Closes the current Apollo Set and creates a new one.
    * request:
    ```js
    {
        "type": "new"
    }
    ```
    * response: identical to the request to app's `/init`.

* `open`:
    * Closes the current Apollo Set and opens an existing one from file.
    * request:
    ```js
    {
        "type": "open",
        "path": string
    }
    ```
    * response: identical to the request to app's `/init`.

* `save`:
    * Saves the current Apollo set to a file. If the file exists, it will be overwritten.
    * request:
    ```js
    {
        "type": "save",
        "path": string
    }
    ```
    * response: identical to the request to app's `/init`.

### `chain` object

The Chain object contains a List of Devices. It is usually found inside a Track or Group object.

* `add`:
    * Adds new instance of device at position in the chain.
    * request: 
    ```js
        {
            "type": "add",
            "index": int,
            "device": string // Device object identifier
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
* `remove`:
    * Removes device at position in the chain.
    * request: 
    ```js
        {
            "type": "remove",
            "index": int
        }
    ```
    * response: 
    ```js
        null
    ```

## app (Electron)

All requests towards the app should target `localhost:1549`. The request URI is different for each kind of request.

* `/init`:
    * .NET Core Host has initialized for the first time and reports Apollo Set information.
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
