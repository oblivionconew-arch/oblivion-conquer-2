#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#include <cstdio>
#include <cstdarg>
#include <cstring>
#include "config.h"

HookConfig g_Config;

static std::string GetModuleDirectory(HMODULE hModule) {
    char path[MAX_PATH] = {0};
    if (GetModuleFileNameA(hModule, path, MAX_PATH) == 0) {
        GetModuleFileNameA(nullptr, path, MAX_PATH);
    }
    char* lastSlash = std::strrchr(path, '\\');
    if (lastSlash) {
        *(lastSlash + 1) = '\0';
    }
    return std::string(path);
}

static bool FileExistsA(const std::string& path) {
    DWORD attr = GetFileAttributesA(path.c_str());
    return (attr != INVALID_FILE_ATTRIBUTES) && !(attr & FILE_ATTRIBUTE_DIRECTORY);
}

void HookLog(const char* fmt, ...) {
    if (!g_Config.enableLog) return;
    std::string logPath = g_Config.baseDir + "conquer_hook.log";
    FILE* f = std::fopen(logPath.c_str(), "a");
    if (!f) return;

    SYSTEMTIME st;
    GetLocalTime(&st);
    std::fprintf(f, "[%02u:%02u:%02u.%03u][PID:%lu] ",
        st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, GetCurrentProcessId());

    va_list args;
    va_start(args, fmt);
    std::vfprintf(f, fmt, args);
    va_end(args);
    std::fprintf(f, "\n");
    std::fclose(f);
}

bool LoadHookConfig(HMODULE hModule) {
    g_Config.baseDir = GetModuleDirectory(hModule);

    std::string iniPath = g_Config.baseDir + "conquer_hook.ini";
    if (!FileExistsA(iniPath)) {
        // Fallback for legacy client installations using csv3config.ini
        std::string legacyPath = g_Config.baseDir + "csv3config.ini";
        if (FileExistsA(legacyPath)) {
            iniPath = legacyPath;
        }
    }

    char hostBuf[256] = {0};
    GetPrivateProfileStringA("config", "host", "127.0.0.1", hostBuf, sizeof(hostBuf), iniPath.c_str());
    g_Config.host = hostBuf;

    // Support both "auth_port" (new) and "port" (legacy csv3config.ini)
    UINT authPort = GetPrivateProfileIntA("config", "auth_port", 0, iniPath.c_str());
    if (authPort == 0) {
        authPort = GetPrivateProfileIntA("config", "port", 9958, iniPath.c_str());
    }
    g_Config.authPort = static_cast<uint16_t>(authPort);

    UINT gamePort = GetPrivateProfileIntA("config", "game_port", 5816, iniPath.c_str());
    g_Config.gamePort = static_cast<uint16_t>(gamePort);

    g_Config.redirectGamePort  = GetPrivateProfileIntA("config", "redirect_game_port", 0, iniPath.c_str()) != 0;
    g_Config.redirectAllRemote = GetPrivateProfileIntA("config", "redirect_all_remote", 1, iniPath.c_str()) != 0;
    g_Config.allowMultiClient  = GetPrivateProfileIntA("config", "multiclient", 1, iniPath.c_str()) != 0;
    g_Config.bypassAntiDebug   = GetPrivateProfileIntA("config", "bypass_antidebug", 1, iniPath.c_str()) != 0;
    g_Config.blockAutoPatch    = GetPrivateProfileIntA("config", "block_autopatch", 1, iniPath.c_str()) != 0;
    g_Config.enableLog         = GetPrivateProfileIntA("config", "log", 0, iniPath.c_str()) != 0;

    HookLog("Loaded config from %s -> host=%s auth_port=%u game_port=%u multi=%d antidebug=%d block_autopatch=%d",
        iniPath.c_str(), g_Config.host.c_str(), g_Config.authPort, g_Config.gamePort,
        g_Config.allowMultiClient ? 1 : 0, g_Config.bypassAntiDebug ? 1 : 0, g_Config.blockAutoPatch ? 1 : 0);

    return true;
}

uint32_t ResolveHostIPv4(const std::string& host) {
    if (host.empty()) return INADDR_NONE;

    // Fast path: dotted IPv4 literal
    unsigned long directIp = inet_addr(host.c_str());
    if (directIp != INADDR_NONE) {
        return static_cast<uint32_t>(directIp);
    }

    // DNS resolution via getaddrinfo (supports domain names like play.myserver.com)
    struct addrinfo hints = {};
    hints.ai_family = AF_INET;
    hints.ai_socktype = SOCK_STREAM;

    struct addrinfo* result = nullptr;
    if (getaddrinfo(host.c_str(), nullptr, &hints, &result) != 0 || !result) {
        HookLog("DNS resolution failed for host '%s' (WSAError=%d)", host.c_str(), WSAGetLastError());
        return INADDR_NONE;
    }

    uint32_t resolved = INADDR_NONE;
    for (struct addrinfo* ptr = result; ptr != nullptr; ptr = ptr->ai_next) {
        if (ptr->ai_family == AF_INET && ptr->ai_addrlen >= sizeof(sockaddr_in)) {
            const auto* sa = reinterpret_cast<const sockaddr_in*>(ptr->ai_addr);
            resolved = sa->sin_addr.s_addr;
            break;
        }
    }
    freeaddrinfo(result);
    return resolved;
}
