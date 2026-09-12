// A deliberately inert substitute for Sysinternals Handle. Never enumerates or closes handles.
#include <windows.h>
#include <string>
#include <cstdio>
int main() {
    wchar_t root[32768];
    if (!GetEnvironmentVariableW(L"APOLLO_WINDOWS_TEST_ROOT", root, 32768)) return 90;
    std::wstring marker = std::wstring(root) + L"\\.windows-updater-test";
    if (GetFileAttributesW(marker.c_str()) == INVALID_FILE_ATTRIBUTES) return 91;
    std::wstring path = std::wstring(root) + L"\\handle-probe.json";
    HANDLE file = CreateFileW(path.c_str(), GENERIC_WRITE, 0, 0, CREATE_ALWAYS, 0, 0);
    if (file == INVALID_HANDLE_VALUE) return 92;
    const char value[] = "{\"substituted\":true,\"doesNotCloseHandles\":true}";
    DWORD written;
    WriteFile(file, value, sizeof(value)-1, &written, 0);
    CloseHandle(file);
    puts("No matching handles found.");
    return 0;
}
