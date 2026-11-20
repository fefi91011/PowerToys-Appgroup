// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"

#include "Generated Files/resource.h"

#include <common/utils/resources.h>
#include <interface/powertoy_module_interface.h>

extern "C" IMAGE_DOS_HEADER __ImageBase;

BOOL APIENTRY DllMain(HMODULE /*hModule*/, DWORD /*ul_reason_for_call*/, LPVOID /*lpReserved*/)
{
    return TRUE;
}

namespace
{
    // Non-localized module key. Must match the settings JSON key and the settings page module name.
    inline const std::wstring ModuleKey = L"AppGroups";
}

// App Groups is a settings-only module: the group shortcuts on the desktop and the taskbar
// are created by the settings page, so the module interface only exposes the enabled state.
class AppGroupsModuleInterface : public PowertoyModuleIface
{
private:
    bool m_enabled = false;

    std::wstring app_name;

    // Contains the non localized key of the powertoy.
    std::wstring app_key;

public:
    AppGroupsModuleInterface()
    {
        app_name = GET_RESOURCE_STRING(IDS_APP_GROUPS_NAME);
        app_key = ModuleKey;
    }

    ~AppGroupsModuleInterface()
    {
        m_enabled = false;
    }

    // Return the localized display name of the powertoy.
    const wchar_t* get_name() override
    {
        return app_name.c_str();
    }

    // Return the non localized key of the powertoy, this will be cached by the runner.
    const wchar_t* get_key() override
    {
        return app_key.c_str();
    }

    // New modules start disabled so that no shortcuts are placed without the user's consent.
    bool is_enabled_by_default() const override
    {
        return false;
    }

    bool get_config(wchar_t* /*buffer*/, int* /*buffer_size*/) override
    {
        return false;
    }

    void set_config(const wchar_t* /*config*/) override
    {
    }

    void call_custom_action(const wchar_t* /*action*/) override
    {
    }

    bool is_enabled() override
    {
        return m_enabled;
    }

    void enable() override
    {
        m_enabled = true;
    }

    void disable() override
    {
        m_enabled = false;
    }

    // Destroy the powertoy and free memory.
    void destroy() override
    {
        delete this;
    }
};

extern "C" __declspec(dllexport) PowertoyModuleIface * __cdecl powertoy_create()
{
    return new AppGroupsModuleInterface();
}
