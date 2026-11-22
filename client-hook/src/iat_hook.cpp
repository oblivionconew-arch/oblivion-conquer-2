#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#include <tlhelp32.h>
#include <cstring>
#include <cstdint>
#include "iat_hook.h"

static bool StrEqualsNoCase(const char* a, const char* b) {
    if (!a || !b) return false;
    return _stricmp(a, b) == 0;
}

bool PatchModuleIAT(HMODULE hModule, const char* targetDll, const char* funcName, void* detourFunc, void** outOriginal) {
    if (!hModule || !targetDll || !funcName || !detourFunc) return false;

    auto* base = reinterpret_cast<uint8_t*>(hModule);
    auto* dos = reinterpret_cast<IMAGE_DOS_HEADER*>(base);
    if (dos->e_magic != IMAGE_DOS_SIGNATURE) return false;

    auto* nt = reinterpret_cast<IMAGE_NT_HEADERS*>(base + dos->e_lfanew);
    if (nt->Signature != IMAGE_NT_SIGNATURE) return false;

    auto& importDir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (importDir.VirtualAddress == 0 || importDir.Size == 0) return false;

    auto* importDesc = reinterpret_cast<IMAGE_IMPORT_DESCRIPTOR*>(base + importDir.VirtualAddress);
    bool patched = false;

    for (; importDesc->Name != 0; ++importDesc) {
        const char* dllName = reinterpret_cast<const char*>(base + importDesc->Name);
        if (!StrEqualsNoCase(dllName, targetDll)) continue;

        auto* origThunk = reinterpret_cast<IMAGE_THUNK_DATA*>(
            base + (importDesc->OriginalFirstThunk ? importDesc->OriginalFirstThunk : importDesc->FirstThunk));
        auto* iatThunk = reinterpret_cast<IMAGE_THUNK_DATA*>(base + importDesc->FirstThunk);

        for (; origThunk->u1.AddressOfData != 0; ++origThunk, ++iatThunk) {
            if (IMAGE_SNAP_BY_ORDINAL(origThunk->u1.Ordinal)) continue;

            auto* importByName = reinterpret_cast<IMAGE_IMPORT_BY_NAME*>(base + origThunk->u1.AddressOfData);
            if (std::strcmp(reinterpret_cast<const char*>(importByName->Name), funcName) != 0) continue;

            DWORD oldProtect = 0;
            if (VirtualProtect(&iatThunk->u1.Function, sizeof(uintptr_t), PAGE_READWRITE, &oldProtect)) {
                if (outOriginal && *outOriginal == nullptr) {
                    *outOriginal = reinterpret_cast<void*>(iatThunk->u1.Function);
                }
                iatThunk->u1.Function = reinterpret_cast<uintptr_t>(detourFunc);
                VirtualProtect(&iatThunk->u1.Function, sizeof(uintptr_t), oldProtect, &oldProtect);
                patched = true;
            }
        }
    }
    return patched;
}

bool PatchAllModulesIAT(HMODULE selfModule, const char* targetDll, const char* funcName, void* detourFunc, void** outOriginal) {
    bool anyPatched = false;
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE, GetCurrentProcessId());
    if (snap == INVALID_HANDLE_VALUE) {
        return PatchModuleIAT(GetModuleHandleA(nullptr), targetDll, funcName, detourFunc, outOriginal);
    }

    MODULEENTRY32 me = {};
    me.dwSize = sizeof(me);
    if (Module32First(snap, &me)) {
        do {
            if (me.hModule == selfModule) continue;
            if (PatchModuleIAT(me.hModule, targetDll, funcName, detourFunc, outOriginal)) {
                anyPatched = true;
            }
        } while (Module32Next(snap, &me));
    }
    CloseHandle(snap);
    return anyPatched;
}

// Minimal x86 prologue instruction length decoder covering standard Win32/Wine API stubs.
// Returns 0 if an instruction is unrecognized or relative-branch sensitive.
static size_t DecodeSafeX86InstructionLen(const uint8_t* p) {
    uint8_t op = p[0];

    // 1-byte push/pop/nop
    if (op == 0x90 || (op >= 0x50 && op <= 0x5F)) return 1;
    // mov edi, edi / mov ebp, esp / mov esp, ebp / xor eax, eax
    if ((op == 0x8B && (p[1] == 0xFF || p[1] == 0xEC || p[1] == 0xE5)) ||
        (op == 0x33 && p[1] == 0xC0)) return 2;
    // push imm8
    if (op == 0x6A) return 2;
    // sub esp, imm8 / add esp, imm8
    if (op == 0x83 && (p[1] == 0xEC || p[1] == 0xC4)) return 3;
    // push imm32 / mov reg32, imm32 (B8..BF)
    if (op == 0x68 || (op >= 0xB8 && op <= 0xBF)) return 5;
    // sub esp, imm32
    if (op == 0x81 && p[1] == 0xEC) return 6;
    // mov eax, fs:[disp32] (64 A1 xx xx xx xx) — common in IsDebuggerPresent / ntdll
    if (op == 0x64 && p[1] == 0xA1) return 6;
    // mov reg32, [ebp+disp8] (8B 45/4D/55/75/7D disp8)
    if (op == 0x8B && (p[1] == 0x45 || p[1] == 0x4D || p[1] == 0x55 || p[1] == 0x75 || p[1] == 0x7D)) return 3;
    // mov eax, [mem32] (A1 imm32)
    if (op == 0xA1) return 5;
    // jmp dword ptr [imm32] (FF 25 imm32) — Windows 10/11 API-set forwarder stub (6 bytes, absolute ptr!)
    if (op == 0xFF && p[1] == 0x25) return 6;
    // mov edx, [imm32] (8B 15 imm32) or mov eax, [imm32] (8B 05 imm32)
    if (op == 0x8B && (p[1] == 0x05 || p[1] == 0x15 || p[1] == 0x0D)) return 6;
    // lea eax/ecx/edx, [esp+disp8] (8D 44/4C/54 24 disp8)
    if (op == 0x8D && (p[1] == 0x44 || p[1] == 0x4C || p[1] == 0x54) && p[2] == 0x24) return 4;

    return 0;
}

bool InstallInlineHookX86(void* targetFunc, void* detourFunc, void** outTrampoline) {
    if (!targetFunc || !detourFunc || !outTrampoline) return false;

    auto* target = reinterpret_cast<uint8_t*>(targetFunc);

    // Follow Win32 hotpatch stub or FF 25 jmp [ptr] if immediate 5-byte relative jmp is not present
    // Calculate how many whole bytes >= 5 we can safely relocate
    size_t stolenLen = 0;
    while (stolenLen < 5) {
        size_t len = DecodeSafeX86InstructionLen(target + stolenLen);
        if (len == 0) {
            return false; // Unsupported prologue; caller will rely on IAT hook
        }
        stolenLen += len;
        // Stop immediately if we hit an unconditional indirect jmp (FF 25)
        if (stolenLen >= 6 && target[stolenLen - 6] == 0xFF && target[stolenLen - 5] == 0x25) {
            break;
        }
    }

    auto* tramp = reinterpret_cast<uint8_t*>(
        VirtualAlloc(nullptr, stolenLen + 16, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE));
    if (!tramp) return false;

    std::memcpy(tramp, target, stolenLen);
    // Append JMP back to (target + stolenLen)
    tramp[stolenLen] = 0xE9;
    int32_t relBack = static_cast<int32_t>((target + stolenLen) - (tramp + stolenLen + 5));
    std::memcpy(tramp + stolenLen + 1, &relBack, sizeof(relBack));

    DWORD oldProtect = 0;
    if (!VirtualProtect(target, stolenLen, PAGE_EXECUTE_READWRITE, &oldProtect)) {
        VirtualFree(tramp, 0, MEM_RELEASE);
        return false;
    }

    target[0] = 0xE9;
    int32_t relDetour = static_cast<int32_t>(reinterpret_cast<uint8_t*>(detourFunc) - (target + 5));
    std::memcpy(target + 1, &relDetour, sizeof(relDetour));
    for (size_t i = 5; i < stolenLen; ++i) {
        target[i] = 0x90; // NOP pad
    }

    FlushInstructionCache(GetCurrentProcess(), target, stolenLen);
    VirtualProtect(target, stolenLen, oldProtect, &oldProtect);

    *outTrampoline = tramp;
    return true;
}
