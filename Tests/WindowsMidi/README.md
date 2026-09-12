# WinMM close error fault injection

This standalone native test compiles the **vendored source** from Native/rtmidi, substituting the WinMM MIDI input functions. It never opens a physical MIDI port. It does not establish the behavior of Apollo's shipped rtmidi.dll, a particular USB driver or Windows 11.

From the repository root, with MinGW-w64 g++:

```powershell
g++ -static -std=c++11 -D__WINDOWS_MM__ Tests/WindowsMidi/WinMMFault.cpp -lwinmm -o artifacts/winmm-fault.exe
./artifacts/winmm-fault.exe healthy
./artifacts/winmm-fault.exe error-state
./artifacts/winmm-fault.exe repeated-close
```

Run each mode as a separate child process, with a timeout. The last mode intentionally exercises an invalid second close and is expected to terminate abnormally on the existing source.

Observed on Windows 10 build 19045, 2026-09-12:

- healthy: exit 0; connected=0, critical-section depth=0, four unprepare calls.
- error-state: inject MMSYSERR_INVALHANDLE; catch the first RtMidiError; connected remains 1 and critical-section depth remains 1 after one unprepare. The fixture exits directly without invoking the broken destructor.
- repeated-close: same first failure, then close again as Apollo's managed Close/Dispose path can do; exit 0xC0000374 (heap corruption).

The first failed cleanup frees a header but leaves stale buffer pointers, connected state and the held critical section. Merely adding LeaveCriticalSection on error is insufficient. A future native fix needs exception-safe, idempotent cleanup that respects pending driver callbacks and buffer ownership. Test MMSYSERR_INVALHANDLE, MIDIERR_STILLPLAYING, each buffer index, repeated Close/Dispose, callbacks and reconnection before validating with physical hardware.

Relevant review: https://github.com/mat1jaczyyy/apollo-studio/pull/486
WinMM buffer contract: https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midiinunprepareheader
