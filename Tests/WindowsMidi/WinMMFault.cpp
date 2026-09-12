// Fault injection against the vendored WinMM backend; no physical MIDI calls.
// This is NOT a reproduction with the shipped rtmidi.dll or a USB Launchpad.
#include <windows.h>
#include <mmsystem.h>
#include <cstdio>
#include <cstring>
static bool removed = false;
static int lockDepth = 0;
static int unprepareCalls = 0;
static UINT WINAPI FakeCount() { return 1; }
static MMRESULT WINAPI FakeOpen(LPHMIDIIN handle, UINT, DWORD_PTR, DWORD_PTR, DWORD) { *handle = (HMIDIIN)1; return MMSYSERR_NOERROR; }
static MMRESULT WINAPI FakeSimple(HMIDIIN) { return MMSYSERR_NOERROR; }
static MMRESULT WINAPI FakeHeader(HMIDIIN, LPMIDIHDR, UINT) { return MMSYSERR_NOERROR; }
static MMRESULT WINAPI FakeUnprepare(HMIDIIN, LPMIDIHDR, UINT) { ++unprepareCalls; return removed ? MMSYSERR_INVALHANDLE : MMSYSERR_NOERROR; }
static void WINAPI TrackedEnter(LPCRITICAL_SECTION cs) { EnterCriticalSection(cs); ++lockDepth; }
static void WINAPI TrackedLeave(LPCRITICAL_SECTION cs) { LeaveCriticalSection(cs); --lockDepth; }
#define midiInGetNumDevs FakeCount
#define midiInOpen FakeOpen
#define midiInReset FakeSimple
#define midiInStop FakeSimple
#define midiInStart FakeSimple
#define midiInClose FakeSimple
#define midiInPrepareHeader FakeHeader
#define midiInAddBuffer FakeHeader
#define midiInUnprepareHeader FakeUnprepare
#define EnterCriticalSection TrackedEnter
#define LeaveCriticalSection TrackedLeave
#include "../../Native/rtmidi/RtMidi.cpp"
int main(int argc, char** argv) {
    SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX | SEM_NOOPENFILEERRORBOX);
    const char* mode = argc > 1 ? argv[1] : "healthy";
    RtMidiIn* input = new RtMidiIn(RtMidi::WINDOWS_MM, "fault fixture");
    input->openPort(0);
    removed = strcmp(mode, "healthy") != 0;
    try { input->closePort(); }
    catch (const RtMidiError& error) { fprintf(stderr, "caught first close: %s\n", error.what()); }
    printf("after first close: connected=%d lockDepth=%d unprepareCalls=%d\n", input->isPortOpen(), lockDepth, unprepareCalls);
    fflush(stdout);
    if (strcmp(mode, "error-state") == 0) {
        int result = input->isPortOpen() && lockDepth == 1 && unprepareCalls == 1 ? 0 : 1;
        // Intentionally abandon only this isolated test process after observing
        // corrupt cleanup state, to avoid a second close in the destructor.
        ExitProcess(result);
    }
    if (strcmp(mode, "repeated-close") == 0) {
        puts("second close follows the managed Close/Dispose retry path");
        fflush(stdout);
        try { input->closePort(); }
        catch (const RtMidiError& error) { fprintf(stderr, "caught second close: %s\n", error.what()); }
        puts("second close unexpectedly survived");
        fflush(stdout);
        ExitProcess(3);
    }
    delete input;
    return lockDepth == 0 ? 0 : 1;
}
