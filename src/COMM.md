# Communication Protocol for Apollo

## api (.NET Core)

All requests towards the api should target `localhost:1548/api`.

### Messages

Generally, a message should be formatted as follows. If the message recipient is a `device`, the device recipient type should also be explicitly stated.

```js
{
    "object": "message",
    "recipient": string, // Recipient object identifier
    "device": string, // If the recipient is a device, include device identifier as well
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

ALL objects, upon attempting to resolve the request, will forward the inner message to the desired member object. You can nest multiple forward messages into each other to access a deep object. The top-most recipient will ALWAYS be the master Set object. 

### Objects

#### `set` object

The Set object contains the List of Tracks. It also does file management and stores the BPM value. It is the top-most object in the Apollo hierarchy, and can only have one instance loaded at a time (it is static).

```js
{
    "object": "set", 
    "bpm": int, 
    "tracks": {
        "count": int,
        "i": api.track // i = 0 to (this."count" - 1)
    }
}
```

* `new`:
    * Closes the current Apollo Set and creates a new one.
    * request:
    ```js
    {
        "type": "new"
    }
    ```
    * response: A JSON-encoded [Set object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#set-object)

* `open`:
    * Closes the current Apollo Set and opens an existing one from file.
    * request:
    ```js
    {
        "type": "open",
        "path": string
    }
    ```
    * response: A JSON-encoded [Set object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#set-object)

* `save`:
    * Saves the current Apollo set to a file. If the file exists, it will be overwritten.
    * request:
    ```js
    {
        "type": "save",
        "path": string
    }
    ```
    * response: A JSON-encoded [Set object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#set-object)

#### `track` object

The Track object contains the top-most Chain of the Track and the currently selected Launchpad used as the Track's input and output. It is found inside of a Set.

```js
{
    "object": "track",
    "data": {
        "chain": api.chain,
        "launchpad": api.launchpad
    }
}
```

The Track object currently handles no requests.

#### `launchpad` object

The Launchpad object represents a physical Launchpad device. It contains the MIDI port names associated with the device's input and output. It is found inside of a Track.

```js
{
    "object": "launchpad",
    "data": {
        "port": string
    }
}
```

The Launchpad object currently handles no requests.

#### `chain` object

The Chain object contains a List of Devices. It is found inside of a Track or Group. Signal data travels through the contained Devices in ascending order (left-to-right), and then exits into the parent object.

```js
{
    "object": "chain",
    "data": {
        "count": int,
        "i": api.device // i = 0 to (this."count" - 1)
    }
}
```

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
 
#### `device` object

The Device object is a generic object that holds a device identifier and the parameters said device holds. It is found inside a Chain.

```js
{
    "object": "device",
    "data": {
        "device:" string, // device identifier
        "data": {
            // device-specific data
        }
    }
}
```

The Device object currently handles no requests.

### Devices

Devices are a subset of the Device object and the most important unit of the Apollo hierarchy. Their job is to process and shape incoming signals to produce proper light effects.

#### `delay` device

The Delay device delays an incoming signal by `length * gate` milliseconds.

```js
{
    "device:" "delay",
    "data": {
        "length": int, // base duration in ms, [10, 30000]
        "gate": Decimal // gate multiplier in %, [0, 4]
    }
}
```

* `length`:
    * Updates length parameter to given value.
    * request: 
    ```js
    {
        "type": "length",
        "value": int // [10, 30000]
    }
    ```
    * response: 
    ```js
    null
    ```

* `gate`:
    * Updates gate parameter to given value.
    * request: 
    ```js
    {
        "type": "gate",
        "value": int // [0, 4]
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
    * request: A JSON-encoded [api.Set object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#set-object)
    * response: 
    ```js
    null
    ```
