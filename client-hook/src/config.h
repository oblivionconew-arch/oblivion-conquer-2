#pragma once

#include <windows.h>
#include <string>
#include <cstdint>

struct HookConfig {
    std::string host;
    uint16_t authPort = 9958;
    uint16_t gamePort = 5816;
    bool redirectGamePort = false;
    bool redirectAllRemote = true;
    bool allowMultiClient = true;
    bool bypassAntiDebug = true;
    bool blockAutoPatch = true;
    bool enableLog = false;
    std::string baseDir;
};

extern HookConfig g_Config;

bool LoadHookConfig(HMODULE hModule);
void HookLog(const char* fmt, ...);
uint32_t ResolveHostIPv4(const std::string& host);
