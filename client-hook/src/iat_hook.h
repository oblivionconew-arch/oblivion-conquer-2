#pragma once

#include <windows.h>

// Patches IAT entries across a single PE module (returns true if at least one entry patched).
bool PatchModuleIAT(HMODULE hModule, const char* targetDll, const char* funcName, void* detourFunc, void** outOriginal);

// Patches IAT across the main executable and all currently loaded modules (excluding our own DLL).
bool PatchAllModulesIAT(HMODULE selfModule, const char* targetDll, const char* funcName, void* detourFunc, void** outOriginal);

// Installs an x86 inline trampoline hook on an exported function (handles hotpatch/standard prologues).
// Falls back cleanly without corrupting instructions if prologue is unsupported.
bool InstallInlineHookX86(void* targetFunc, void* detourFunc, void** outTrampoline);
