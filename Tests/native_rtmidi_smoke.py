"""Exercise Apollo's bundled RtMidi C ABI with a macOS virtual MIDI loopback."""
import ctypes as c
from pathlib import Path
import sys
import threading
import time


def smoke(path):
    lib = c.CDLL(str(Path(path).resolve()))
    ptr = c.c_void_p
    class Wrapper(c.Structure):
        _fields_ = [("ptr", ptr), ("data", ptr), ("ok", c.c_bool), ("msg", ptr)]

    def check(device):
        assert device, "Native MIDI wrapper allocation failed"
        state = c.cast(device, c.POINTER(Wrapper)).contents
        assert state.ptr and state.ok, "Native MIDI operation failed"

    callback_type = c.CFUNCTYPE(None, c.c_double, ptr, c.c_size_t, ptr)
    signatures = {
        "rtmidi_sizeof_rtmidi_api": (c.c_int, []),
        "rtmidi_get_compiled_api": (c.c_int, [c.POINTER(c.c_int), c.c_uint]),
        "rtmidi_in_create_default": (ptr, []), "rtmidi_out_create_default": (ptr, []),
        "rtmidi_in_free": (None, [ptr]), "rtmidi_out_free": (None, [ptr]),
        "rtmidi_in_cancel_callback": (None, [ptr]),
        "rtmidi_open_virtual_port": (None, [ptr, c.c_char_p]),
        "rtmidi_get_port_count": (c.c_uint, [ptr]),
        "rtmidi_get_port_name": (ptr, [ptr, c.c_uint]),
        "rtmidi_open_port": (None, [ptr, c.c_uint, c.c_char_p]),
        "rtmidi_in_set_callback": (None, [ptr, callback_type, ptr]),
        "rtmidi_out_send_message": (c.c_int, [ptr, c.POINTER(c.c_ubyte), c.c_int]),
    }
    for name, (result, args) in signatures.items():
        function = getattr(lib, name)
        function.restype, function.argtypes = result, args
    assert lib.rtmidi_sizeof_rtmidi_api() == c.sizeof(c.c_int)
    count = lib.rtmidi_get_compiled_api(None, 0)
    assert 0 < count < 32, "Invalid compiled API count"
    apis = (c.c_int * count)()
    assert lib.rtmidi_get_compiled_api(apis, len(apis)) > 0 and 1 in apis, "CoreMIDI backend missing"
    native_free = c.CDLL(None).free
    native_free.argtypes = [ptr]
    native_free.restype = None
    incoming, outgoing = lib.rtmidi_in_create_default(), lib.rtmidi_out_create_default()
    check(incoming)
    check(outgoing)
    received = threading.Event()
    expected = bytes([0x90, 60, 100])

    @callback_type
    def callback(timestamp, data, length, context):
        if c.string_at(data, length) == expected:
            received.set()

    try:
        name = b"Apollo ARM build verification"
        lib.rtmidi_open_virtual_port(outgoing, name)
        check(outgoing)
        port = None
        deadline = time.monotonic() + 5
        while port is None and time.monotonic() < deadline:
            count = lib.rtmidi_get_port_count(incoming)
            assert count < 4096, "Native MIDI port enumeration failed"
            for index in range(count):
                address = lib.rtmidi_get_port_name(incoming, index)
                # Failed calls return a static string, which must not be freed.
                check(incoming)
                if address:
                    try:
                        if name in c.string_at(address):
                            port = index
                    finally:
                        native_free(address)
            if port is None:
                time.sleep(0.05)
        assert port is not None, "Virtual MIDI source was not discovered"
        lib.rtmidi_in_set_callback(incoming, callback, None)
        lib.rtmidi_open_port(incoming, port, b"Apollo verification input")
        check(incoming)
        message = (c.c_ubyte * len(expected))(*expected)
        assert lib.rtmidi_out_send_message(outgoing, message, len(message)) == 0
        assert received.wait(5), "Virtual MIDI message did not arrive"
    finally:
        lib.rtmidi_in_cancel_callback(incoming)
        lib.rtmidi_in_free(incoming)
        lib.rtmidi_out_free(outgoing)
    print("PASS native CoreMIDI ABI, port discovery, callback and note-message loopback")


if __name__ == "__main__":
    smoke(sys.argv[1])
