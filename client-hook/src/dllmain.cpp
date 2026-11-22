#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#include <unknwn.h>
#include <shellapi.h>
#include <cstdio>
#include <cstring>
#include <cwctype>
#include <string>
#include <algorithm>
#include "config.h"
#include "iat_hook.h"

#ifndef NTSTATUS
typedef LONG NTSTATUS;
#endif

// Original function pointer types
typedef int    (WSAAPI* Connect_t)(SOCKET s, const struct sockaddr* name, int namelen);
typedef HANDLE (WINAPI* CreateMutexA_t)(LPSECURITY_ATTRIBUTES, BOOL, LPCSTR);
typedef HANDLE (WINAPI* CreateMutexW_t)(LPSECURITY_ATTRIBUTES, BOOL, LPCWSTR);
typedef HANDLE (WINAPI* OpenMutexA_t)(DWORD, BOOL, LPCSTR);
typedef HANDLE (WINAPI* OpenMutexW_t)(DWORD, BOOL, LPCWSTR);
typedef BOOL   (WINAPI* IsDebuggerPresent_t)(void);
typedef BOOL   (WINAPI* CheckRemoteDebuggerPresent_t)(HANDLE, PBOOL);
typedef NTSTATUS (NTAPI* NtQueryInformationProcess_t)(HANDLE, ULONG, PVOID, ULONG, PULONG);
typedef HINSTANCE (WINAPI* ShellExecuteA_t)(HWND, LPCSTR, LPCSTR, LPCSTR, LPCSTR, INT);
typedef HINSTANCE (WINAPI* ShellExecuteW_t)(HWND, LPCWSTR, LPCWSTR, LPCWSTR, LPCWSTR, INT);
typedef int       (WINAPI* MessageBoxA_t)(HWND, LPCSTR, LPCSTR, UINT);
typedef int       (WINAPI* MessageBoxW_t)(HWND, LPCWSTR, LPCWSTR, UINT);

static Connect_t                    g_OrigConnect                    = nullptr;
static CreateMutexA_t               g_OrigCreateMutexA               = nullptr;
static CreateMutexW_t               g_OrigCreateMutexW               = nullptr;
static OpenMutexA_t                 g_OrigOpenMutexA                 = nullptr;
static OpenMutexW_t                 g_OrigOpenMutexW                 = nullptr;
static IsDebuggerPresent_t          g_OrigIsDebuggerPresent          = nullptr;
static CheckRemoteDebuggerPresent_t g_OrigCheckRemoteDebuggerPresent = nullptr;
static NtQueryInformationProcess_t  g_OrigNtQueryInformationProcess  = nullptr;
static ShellExecuteA_t              g_OrigShellExecuteA              = nullptr;
static ShellExecuteW_t              g_OrigShellExecuteW              = nullptr;
static MessageBoxA_t                g_OrigMessageBoxA                = nullptr;
static MessageBoxW_t                g_OrigMessageBoxW                = nullptr;

static HMODULE g_SelfModule = nullptr;

// Helper: case-insensitive substring match (ASCII)
static bool ContainsNoCaseA(const char* haystack, const char* needle) {
    if (!haystack || !needle) return false;
    std::string h(haystack);
    std::string n(needle);
    std::transform(h.begin(), h.end(), h.begin(), [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
    std::transform(n.begin(), n.end(), n.begin(), [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
    return h.find(n) != std::string::npos;
}

static bool ContainsNoCaseW(const wchar_t* haystack, const wchar_t* needle) {
    if (!haystack || !needle) return false;
    std::wstring h(haystack);
    std::wstring n(needle);
    std::transform(h.begin(), h.end(), h.begin(), [](wchar_t c) { return static_cast<wchar_t>(std::towlower(c)); });
    std::transform(n.begin(), n.end(), n.begin(), [](wchar_t c) { return static_cast<wchar_t>(std::towlower(c)); });
    return h.find(n) != std::wstring::npos;
}

static bool IsConquerMutexA(LPCSTR lpName) {
    if (!lpName || lpName[0] == '\0') return false;
    return ContainsNoCaseA(lpName, "conquer") ||
           ContainsNoCaseA(lpName, "tq_") ||
           ContainsNoCaseA(lpName, "mutexforassist") ||
           ContainsNoCaseA(lpName, "co_mutex") ||
           ContainsNoCaseA(lpName, "soul_") ||
           ContainsNoCaseA(lpName, "client_mutex");
}

static bool IsConquerMutexW(LPCWSTR lpName) {
    if (!lpName || lpName[0] == L'\0') return false;
    return ContainsNoCaseW(lpName, L"conquer") ||
           ContainsNoCaseW(lpName, L"tq_") ||
           ContainsNoCaseW(lpName, L"mutexforassist") ||
           ContainsNoCaseW(lpName, L"co_mutex") ||
           ContainsNoCaseW(lpName, L"soul_") ||
           ContainsNoCaseW(lpName, L"client_mutex");
}

static bool IsAuthPortCandidate(uint16_t port) {
    return port == 9958 || port == 9959 || port == 9960 || port == 9957;
}

// ============================================================================
// 1. Network Hook: ws2_32.dll!connect
// ============================================================================
static int WSAAPI HookedConnect(SOCKET s, const struct sockaddr* name, int namelen) {
    if (name && name->sa_family == AF_INET && namelen >= static_cast<int>(sizeof(sockaddr_in))) {
        sockaddr_in modified = *reinterpret_cast<const sockaddr_in*>(name);
        uint16_t origPort = ntohs(modified.sin_port);
        char origIpStr[64] = {0};
        inet_ntop(AF_INET, &modified.sin_addr, origIpStr, sizeof(origIpStr));

        bool isLoopback = (modified.sin_addr.s_addr == htonl(INADDR_LOOPBACK));
        bool shouldRedirectIp = IsAuthPortCandidate(origPort) || (g_Config.redirectAllRemote && !isLoopback);

        if (shouldRedirectIp && !g_Config.host.empty()) {
            uint32_t targetIp = ResolveHostIPv4(g_Config.host);
            if (targetIp != INADDR_NONE) {
                modified.sin_addr.s_addr = targetIp;

                if (IsAuthPortCandidate(origPort)) {
                    modified.sin_port = htons(g_Config.authPort);
                } else if (g_Config.redirectGamePort && origPort == 5816) {
                    modified.sin_port = htons(g_Config.gamePort);
                }

                char newIpStr[64] = {0};
                inet_ntop(AF_INET, &modified.sin_addr, newIpStr, sizeof(newIpStr));
                HookLog("connect() redirected %s:%u -> %s:%u",
                    origIpStr, origPort, newIpStr, ntohs(modified.sin_port));

                return g_OrigConnect(s, reinterpret_cast<const struct sockaddr*>(&modified), sizeof(modified));
            }
        }
        HookLog("connect() passthrough %s:%u", origIpStr, origPort);
    }
    return g_OrigConnect(s, name, namelen);
}

// ============================================================================
// 2. Multi-Client Hooks: CreateMutexA/W & OpenMutexA/W
// ============================================================================
static HANDLE WINAPI HookedCreateMutexA(LPSECURITY_ATTRIBUTES attrs, BOOL initialOwner, LPCSTR lpName) {
    if (g_Config.allowMultiClient && IsConquerMutexA(lpName)) {
        char uniqueName[256] = {0};
        std::snprintf(uniqueName, sizeof(uniqueName), "%s_MC_%lu", lpName, GetCurrentProcessId());
        HookLog("CreateMutexA('%s') -> uniquified as '%s'", lpName, uniqueName);
        return g_OrigCreateMutexA(attrs, initialOwner, uniqueName);
    }
    return g_OrigCreateMutexA(attrs, initialOwner, lpName);
}

static HANDLE WINAPI HookedCreateMutexW(LPSECURITY_ATTRIBUTES attrs, BOOL initialOwner, LPCWSTR lpName) {
    if (g_Config.allowMultiClient && IsConquerMutexW(lpName)) {
        wchar_t uniqueName[256] = {0};
        std::swprintf(uniqueName, 256, L"%ls_MC_%lu", lpName, GetCurrentProcessId());
        HookLog("CreateMutexW() -> uniquified for PID %lu", GetCurrentProcessId());
        return g_OrigCreateMutexW(attrs, initialOwner, uniqueName);
    }
    return g_OrigCreateMutexW(attrs, initialOwner, lpName);
}

static HANDLE WINAPI HookedOpenMutexA(DWORD desiredAccess, BOOL inheritHandle, LPCSTR lpName) {
    if (g_Config.allowMultiClient && IsConquerMutexA(lpName)) {
        HookLog("OpenMutexA('%s') -> bypassed for multi-client", lpName);
        SetLastError(ERROR_FILE_NOT_FOUND);
        return nullptr;
    }
    return g_OrigOpenMutexA(desiredAccess, inheritHandle, lpName);
}

static HANDLE WINAPI HookedOpenMutexW(DWORD desiredAccess, BOOL inheritHandle, LPCWSTR lpName) {
    if (g_Config.allowMultiClient && IsConquerMutexW(lpName)) {
        HookLog("OpenMutexW() -> bypassed for multi-client");
        SetLastError(ERROR_FILE_NOT_FOUND);
        return nullptr;
    }
    return g_OrigOpenMutexW(desiredAccess, inheritHandle, lpName);
}

// ============================================================================
// 3. Anti-Debug / Integrity Neutralization Hooks
// ============================================================================
static BOOL WINAPI HookedIsDebuggerPresent(void) {
    return FALSE;
}

static BOOL WINAPI HookedCheckRemoteDebuggerPresent(HANDLE hProcess, PBOOL pbDebuggerPresent) {
    if (pbDebuggerPresent) {
        *pbDebuggerPresent = FALSE;
    }
    return TRUE;
}

static NTSTATUS NTAPI HookedNtQueryInformationProcess(
    HANDLE ProcessHandle,
    ULONG ProcessInformationClass,
    PVOID ProcessInformation,
    ULONG ProcessInformationLength,
    PULONG ReturnLength)
{
    if (g_Config.bypassAntiDebug && ProcessInformation) {
        // ProcessDebugPort (7)
        if (ProcessInformationClass == 7 && ProcessInformationLength >= sizeof(uintptr_t)) {
            *reinterpret_cast<uintptr_t*>(ProcessInformation) = 0;
            if (ReturnLength) *ReturnLength = sizeof(uintptr_t);
            return 0; // STATUS_SUCCESS
        }
        // ProcessDebugObjectHandle (30)
        if (ProcessInformationClass == 30 && ProcessInformationLength >= sizeof(HANDLE)) {
            *reinterpret_cast<HANDLE*>(ProcessInformation) = nullptr;
            if (ReturnLength) *ReturnLength = sizeof(HANDLE);
            return static_cast<NTSTATUS>(0xC0000353L); // STATUS_PORT_NOT_SET
        }
        // ProcessDebugFlags (31) -> 1 means NOT being debugged
        if (ProcessInformationClass == 31 && ProcessInformationLength >= sizeof(ULONG)) {
            *reinterpret_cast<ULONG*>(ProcessInformation) = 1;
            if (ReturnLength) *ReturnLength = sizeof(ULONG);
            return 0; // STATUS_SUCCESS
        }
    }
    return g_OrigNtQueryInformationProcess(
        ProcessHandle, ProcessInformationClass, ProcessInformation, ProcessInformationLength, ReturnLength);
}

// ============================================================================
// 4. AutoPatch / Browser Popup Blocking Hooks
// ============================================================================
static HINSTANCE WINAPI HookedShellExecuteA(HWND hwnd, LPCSTR op, LPCSTR file, LPCSTR params, LPCSTR dir, INT showCmd) {
    if (g_Config.blockAutoPatch && file) {
        if (ContainsNoCaseA(file, "autopatch") ||
            ContainsNoCaseA(file, "http://") ||
            ContainsNoCaseA(file, "https://") ||
            ContainsNoCaseA(file, "play.exe"))
        {
            HookLog("ShellExecuteA('%s') blocked", file);
            return reinterpret_cast<HINSTANCE>(33);
        }
    }
    return g_OrigShellExecuteA(hwnd, op, file, params, dir, showCmd);
}

static HINSTANCE WINAPI HookedShellExecuteW(HWND hwnd, LPCWSTR op, LPCWSTR file, LPCWSTR params, LPCWSTR dir, INT showCmd) {
    if (g_Config.blockAutoPatch && file) {
        if (ContainsNoCaseW(file, L"autopatch") ||
            ContainsNoCaseW(file, L"http://") ||
            ContainsNoCaseW(file, L"https://") ||
            ContainsNoCaseW(file, L"play.exe"))
        {
            HookLog("ShellExecuteW() blocked");
            return reinterpret_cast<HINSTANCE>(33);
        }
    }
    return g_OrigShellExecuteW(hwnd, op, file, params, dir, showCmd);
}

// ============================================================================
// 5. Popup Suppression Hooks (Flash 6.0 warning auto-suppression)
// ============================================================================
static int WINAPI HookedMessageBoxA(HWND hwnd, LPCSTR text, LPCSTR caption, UINT type) {
    if (text && (ContainsNoCaseA(text, "Flash") || ContainsNoCaseA(text, "Adobe Flash"))) {
        HookLog("Suppressed Flash MessageBoxA: '%s'", text);
        return IDOK;
    }
    if (g_OrigMessageBoxA) return g_OrigMessageBoxA(hwnd, text, caption, type);
    return MessageBoxA(hwnd, text, caption, type);
}

static int WINAPI HookedMessageBoxW(HWND hwnd, LPCWSTR text, LPCWSTR caption, UINT type) {
    if (text && (ContainsNoCaseW(text, L"Flash") || ContainsNoCaseW(text, L"Adobe Flash"))) {
        HookLog("Suppressed Flash MessageBoxW");
        return IDOK;
    }
    if (g_OrigMessageBoxW) return g_OrigMessageBoxW(hwnd, text, caption, type);
    return MessageBoxW(hwnd, text, caption, type);
}

// ============================================================================
// Helper: Hook both Inline (for GetProcAddress callers) and IAT (guaranteed fallback)
// ============================================================================
template <typename FuncT>
static void InstallHybridHook(const char* dllName, const char* procName, void* detour, FuncT* outOrig) {
    HMODULE hDll = GetModuleHandleA(dllName);
    if (!hDll) {
        hDll = LoadLibraryA(dllName);
    }

    void* origPtr = nullptr;
    bool inlineOk = false;
    if (hDll) {
        void* target = reinterpret_cast<void*>(GetProcAddress(hDll, procName));
        if (target) {
            inlineOk = InstallInlineHookX86(target, detour, &origPtr);
        }
    }

    if (inlineOk && origPtr) {
        *outOrig = reinterpret_cast<FuncT>(origPtr);
        HookLog("Installed inline hook: %s!%s", dllName, procName);
        return;
    }

    // Fallback: patch IAT across all loaded modules
    void* iatOrig = nullptr;
    bool iatOk = PatchAllModulesIAT(g_SelfModule, dllName, procName, detour, &iatOrig);
    if (iatOrig) {
        *outOrig = reinterpret_cast<FuncT>(iatOrig);
    } else if (hDll) {
        *outOrig = reinterpret_cast<FuncT>(GetProcAddress(hDll, procName));
    }
    HookLog("Installed IAT hook (%s): %s!%s", iatOk ? "OK" : "NOT_IMPORTED", dllName, procName);
}

static void InitializeHooks() {
    LoadHookConfig(g_SelfModule);

    InstallHybridHook("ws2_32.dll", "connect", reinterpret_cast<void*>(&HookedConnect), &g_OrigConnect);

    if (g_Config.allowMultiClient) {
        InstallHybridHook("kernel32.dll", "CreateMutexA", reinterpret_cast<void*>(&HookedCreateMutexA), &g_OrigCreateMutexA);
        InstallHybridHook("kernel32.dll", "CreateMutexW", reinterpret_cast<void*>(&HookedCreateMutexW), &g_OrigCreateMutexW);
        InstallHybridHook("kernel32.dll", "OpenMutexA",   reinterpret_cast<void*>(&HookedOpenMutexA),   &g_OrigOpenMutexA);
        InstallHybridHook("kernel32.dll", "OpenMutexW",   reinterpret_cast<void*>(&HookedOpenMutexW),   &g_OrigOpenMutexW);
    }

    if (g_Config.bypassAntiDebug) {
        InstallHybridHook("kernel32.dll", "IsDebuggerPresent",          reinterpret_cast<void*>(&HookedIsDebuggerPresent),          &g_OrigIsDebuggerPresent);
        InstallHybridHook("kernel32.dll", "CheckRemoteDebuggerPresent", reinterpret_cast<void*>(&HookedCheckRemoteDebuggerPresent), &g_OrigCheckRemoteDebuggerPresent);
        InstallHybridHook("ntdll.dll",    "NtQueryInformationProcess",  reinterpret_cast<void*>(&HookedNtQueryInformationProcess),  &g_OrigNtQueryInformationProcess);
    }

    if (g_Config.blockAutoPatch) {
        InstallHybridHook("shell32.dll", "ShellExecuteA", reinterpret_cast<void*>(&HookedShellExecuteA), &g_OrigShellExecuteA);
        InstallHybridHook("shell32.dll", "ShellExecuteW", reinterpret_cast<void*>(&HookedShellExecuteW), &g_OrigShellExecuteW);
    }

    // Suppress legacy Adobe Flash ActiveX popups on modern Windows 10/11
    InstallHybridHook("user32.dll", "MessageBoxA", reinterpret_cast<void*>(&HookedMessageBoxA), &g_OrigMessageBoxA);
    InstallHybridHook("user32.dll", "MessageBoxW", reinterpret_cast<void*>(&HookedMessageBoxW), &g_OrigMessageBoxW);

    HookLog("ConquerHook.dll initialization complete.");
}

// Proxy forwarder for DINPUT8.dll (loads natively in Conquer.exe without any injector or antivirus flags)
typedef HRESULT (WINAPI* DirectInput8Create_t)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
static DirectInput8Create_t g_OrigDirectInput8Create = nullptr;

extern "C" __declspec(dllexport) HRESULT WINAPI DirectInput8Create(
    HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
    if (!g_OrigDirectInput8Create) {
        char sysPath[MAX_PATH] = {0};
        GetSystemDirectoryA(sysPath, MAX_PATH);
        std::strcat(sysPath, "\\dinput8.dll");
        HMODULE hReal = LoadLibraryA(sysPath);
        if (hReal) {
            g_OrigDirectInput8Create = reinterpret_cast<DirectInput8Create_t>(GetProcAddress(hReal, "DirectInput8Create"));
        }
    }
    if (g_OrigDirectInput8Create) {
        return g_OrigDirectInput8Create(hinst, dwVersion, riidltf, ppvOut, punkOuter);
    }
    return E_FAIL;
}

// Optional export so ConquerHook.dll can also be statically added to Conquer.exe Import Table (CFF Explorer / Stud_PE)
extern "C" __declspec(dllexport) void WINAPI ConquerHookInit() {
    // Initialization already performed in DllMain
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID /*lpReserved*/) {
    if (ul_reason_for_call == DLL_PROCESS_ATTACH) {
        g_SelfModule = hModule;
        DisableThreadLibraryCalls(hModule);
        InitializeHooks();
    }
    return TRUE;
}
