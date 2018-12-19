# Communication Protocol for Apollo

## api (.NET Core)

All requests towards the api should target `localhost:1548/api`. Sending an empty request will return a 200 OK, and should be used as an initial "handshake".

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
    "tracks": array(api.track)
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
    * Saves the current Apollo set. If the file exists, it will be overwritten. If the internal path is empty, the response will be a 400.
    * request:
    ```js
    {
        "type": "save",
    }
    ```
    * response:
    ```js
    null
    ```

* `save_as`:
    * Saves the current Apollo set to a different file. If the file exists, it will be overwritten.
    * request:
    ```js
    {
        "type": "save_as",
        "path": string
    }
    ```
    * response: A JSON-encoded [Set object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#set-object)

#### `midi` object

The MIDI object contains a list of available Launchpads. It can be forwarded to from a Set.

```js
{
    "object": "midi",
    "data": array(api.launchpad)
}
```

* `rescan`:
    * Rescans for new MIDI devices and returns the list.
    * request:
    ```js
    {
        "type": "rescan"
    }
    ```
    * response: A JSON-encoded [MIDI object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#midi-object)

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

* `port`:
    * Changes port of Track to given index, corresponding to the index found in MIDI.
    * request:
    ```js
    {
        "type": "port",
        "index": int
    }
    ```
    * response: A JSON-encoded [Launchpad object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#launchpad-object)

#### `chain` object

The Chain object contains a List of Devices. It is found inside of a Track or Group. Signal data travels through the contained Devices in ascending order (left-to-right), and then exits into the parent object.

```js
{
    "object": "chain",
    "data": array(api.device)
}
```

* `add`:
    * Adds new instance of device at position in the chain.
    * request: 
    ```js
    {
        "type": "add",
        "device": string, // Device object identifier
        "index": int
    }
    ```
    * response: A JSON-encoded [Device object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#device-object)

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
        "device": string, // device identifier
        "data": {
            // device-specific data
        }
    }
}
```

The Device object currently handles no requests.

#### `signal` object

The Signal object travels through the chain from device to device. It is created from an incoming note from the Launchpad, and is converted into SysEx before outputting back to the Launchpad. It contains a button index and the corresponding color object.

```js
{
    "object": "signal",
    "data": {
        "index": int, // [0, 99]
        "color": api.color
    }
}
```

#### `color` object

The Color object holds a definition for a color suitable for showing on the Launchpad. It has 3 6-bit color channels. It is usually found inside devices that modify the color of an incoming Signal (such as Paint).

```js
{
    "object": "color",
    "data": {
        "red": int, // [0, 63]
        "green": int, // [0, 63]
        "blue": int // [0, 63]
    }
}
```

The Color object currently handles no requests.

### Devices

Devices are a subset of the Device object and the most important unit of the Apollo hierarchy. Their job is to process and shape incoming signals to produce proper light effects.

#### `group` device

The Group device contains multiple `chain` objects. Any incoming signal is transmitted to every Chain without any kind of filtering. Any outgoing signal coming from the Chains is transmitted outside into the next device.

```js
{
    "device": "group",
    "data": array(api.chain)
}
```

* `add`:
    * Adds new Chain at position in the List of Chain.
    * request: 
    ```js
    {
        "type": "add",
        "index": int
    }
    ```
    * response: A JSON-encoded [Chain object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#chain-object)

* `remove`:
    * Removes existing Chain at position in the List of Chain.
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

#### `delay` device

The Delay device delays an incoming signal by `length * gate` milliseconds.

```js
{
    "device": "delay",
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
    * response: A JSON-encoded [Delay device](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#delay-device)

* `gate`:
    * Updates gate parameter to given value.
    * request: 
    ```js
    {
        "type": "gate",
        "value": int // [0, 4]
    }
    ```
    * response: A JSON-encoded [Delay device](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#delay-device)

#### `layer` device

The Layer device applies a target layer index to incoming signals.

```js
{
    "device": "layer",
    "data": {
        "target": int // [-∞, +∞]
    }
}
```

* `target`:
    * Updates target to given value.
    * request: 
    ```js
    {
        "type": "target",
        "value": int // [-∞, +∞]
    }
    ```
    * response: A JSON-encoded [Layer device](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#layer-device)

#### `paint` device

The Paint device applies a specific color to incoming signals if they are lit.

```js
{
    "device": "paint",
    "data": {
        "color": api.color
    }
}
```

* `color`:
    * Updates color to given value.
    * request: 
    ```js
    {
        "type": "color",
        "red": int, // [0, 63]
        "green": int, // [0, 63]
        "blue": int // [0, 63]
    }
    ```
    * response: A JSON-encoded [Paint device](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#paint-device)

#### `preview` device

The Preview device uses a grid to display incoming signals and can be used to generate signals manually inside a Chain.

```js
{
    "device": "preview",
    "data": {}
}
```

* `signal`:
    * Generates a Signal and outputs it to the right. Requested upon clicking a button on the grid.
    * request: 
    ```js
    {
        "type": "signal",
        "index": int // [0, 99]
    }
    ```
    * response: A JSON-encoded [Signal object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#signal-object)

#### `translation` device

The Translation device applies an index offset to incoming signals.

```js
{
    "device": "translation",
    "data": {
        "offset": int // [-99, 99]
    }
}
```

* `offset`:
    * Updates offset to given value.
    * request: 
    ```js
    {
        "type": "offset",
        "value": int // [-99, 99]
    }
    ```
    * response: A JSON-encoded [Translation device](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#translation-device)

## app (Electron)

All requests towards the app should target `localhost:1549`. The request URI is different for each kind of request.

* `/init`:
    * .NET Core Host has initialized for the first time and reports Apollo Set information.
    * request: A JSON-encoded [api.Set object](https://github.com/mat1jaczyyy/apollo-studio/blob/master/src/COMM.md#set-object)
    * response: 
    ```js
    null
    ```
