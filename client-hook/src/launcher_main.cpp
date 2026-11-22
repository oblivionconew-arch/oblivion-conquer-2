#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#include <string>
#include <cstring>

static std::string GetSelfDir() {
    char path[MAX_PATH] = {0};
    GetModuleFileNameA(nullptr, path, MAX_PATH);
    char* slash = std::strrchr(path, '\\');
    if (slash) *(slash + 1) = '\0';
    return std::string(path);
}

static bool FileExists(const std::string& p) {
    DWORD a = GetFileAttributesA(p.c_str());
    return (a != INVALID_FILE_ATTRIBUTES) && !(a & FILE_ATTRIBUTE_DIRECTORY);
}

int WINAPI WinMain(HINSTANCE /*hInst*/, HINSTANCE /*hPrev*/, LPSTR /*lpCmdLine*/, int /*nShowCmd*/) {
    std::string dir = GetSelfDir();
    std::string exePath = dir + "Conquer.exe";
    std::string dllPath = dir + "ConquerHook.dll";

    if (!FileExists(exePath)) {
        MessageBoxA(nullptr,
            "Conquer.exe nao encontrado nesta pasta.\nColoque PlayLauncher.exe e ConquerHook.dll na pasta principal do cliente.",
            "Conquer Launcher", MB_ICONERROR | MB_OK);
        return 1;
    }

    if (!FileExists(dllPath)) {
        MessageBoxA(nullptr,
            "ConquerHook.dll nao encontrada nesta pasta.\nVerifique se ConquerHook.dll esta ao lado do PlayLauncher.exe.",
            "Conquer Launcher", MB_ICONERROR | MB_OK);
        return 1;
    }

    // Launch Conquer.exe with "blacknull" argument (skips AutoPatch check) in suspended state
    std::string cmdLine = "\"" + exePath + "\" blacknull";
    char cmdBuf[MAX_PATH * 2] = {0};
    std::strncpy(cmdBuf, cmdLine.c_str(), sizeof(cmdBuf) - 1);

    STARTUPINFOA si = {};
    si.cb = sizeof(si);
    PROCESS_INFORMATION pi = {};

    if (!CreateProcessA(
            exePath.c_str(),
            cmdBuf,
            nullptr,
            nullptr,
            FALSE,
            CREATE_SUSPENDED,
            nullptr,
            dir.c_str(),
            &si,
            &pi))
    {
        MessageBoxA(nullptr,
            "Falha ao iniciar Conquer.exe (CreateProcessA).\nTente executar como Administrador se o cliente estiver em Arquivos de Programas.",
            "Conquer Launcher", MB_ICONERROR | MB_OK);
        return 2;
    }

    // Inject ConquerHook.dll before the main thread runs
    size_t allocSize = dllPath.size() + 1;
    void* remoteMem = VirtualAllocEx(pi.hProcess, nullptr, allocSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    if (!remoteMem) {
        TerminateProcess(pi.hProcess, 1);
        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);
        MessageBoxA(nullptr, "Falha ao alocar memoria no processo Conquer.exe.", "Conquer Launcher", MB_ICONERROR | MB_OK);
        return 3;
    }

    if (!WriteProcessMemory(pi.hProcess, remoteMem, dllPath.c_str(), allocSize, nullptr)) {
        VirtualFreeEx(pi.hProcess, remoteMem, 0, MEM_RELEASE);
        TerminateProcess(pi.hProcess, 1);
        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);
        MessageBoxA(nullptr, "Falha ao gravar caminho da DLL no Conquer.exe.", "Conquer Launcher", MB_ICONERROR | MB_OK);
        return 4;
    }

    HMODULE hKernel32 = GetModuleHandleA("kernel32.dll");
    auto loadLibAddr = reinterpret_cast<LPTHREAD_START_ROUTINE>(GetProcAddress(hKernel32, "LoadLibraryA"));
    HANDLE hThread = CreateRemoteThread(pi.hProcess, nullptr, 0, loadLibAddr, remoteMem, 0, nullptr);
    if (!hThread) {
        VirtualFreeEx(pi.hProcess, remoteMem, 0, MEM_RELEASE);
        TerminateProcess(pi.hProcess, 1);
        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);
        MessageBoxA(nullptr, "Falha ao injetar ConquerHook.dll (CreateRemoteThread).", "Conquer Launcher", MB_ICONERROR | MB_OK);
        return 5;
    }

    WaitForSingleObject(hThread, 10000);
    DWORD injectedModule = 0;
    GetExitCodeThread(hThread, &injectedModule);
    CloseHandle(hThread);
    VirtualFreeEx(pi.hProcess, remoteMem, 0, MEM_RELEASE);

    if (injectedModule == 0) {
        TerminateProcess(pi.hProcess, 1);
        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);
        MessageBoxA(nullptr,
            "ConquerHook.dll falhou ao carregar no Conquer.exe.\nVerifique se a DLL foi compilada em 32-bit (x86) Release /MT.",
            "Conquer Launcher", MB_ICONERROR | MB_OK);
        return 6;
    }

    ResumeThread(pi.hThread);
    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);
    return 0;
}
