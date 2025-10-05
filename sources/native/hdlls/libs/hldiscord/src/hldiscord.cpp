#define HL_NAME(n) discord_##n
#include <hl.h>
#include <hlsystem.h>
#include <tchar.h>
#include <discord_rpc.h>
#include <time.h>


void Ready(const DiscordUser* request) {
}

void Disconnected(int _errorCode, const char* _message) {
}

void Errored(int _errorCode, const char* _message) {
}

static DiscordRichPresence presence;
static bool init = false;
HL_PRIM bool HL_NAME(init)(vbyte* _appID, vbyte* _steamID){
    static DiscordEventHandlers handlers;
    if( init ) return true;

    memset(&handlers, 0, sizeof(handlers));
    handlers.ready = &Ready;
    handlers.disconnected = &Disconnected;
    handlers.errored = &Errored;
    init = true;
    Discord_Initialize((const char*)_appID, &handlers, 1, (const char*)_steamID);
    memset(&presence, 0, sizeof(presence));
    time_t now;
    presence.startTimestamp = time(&now);
    return true;
}

HL_PRIM void HL_NAME(update_presence)() {
    Discord_UpdatePresence(&presence);
}

HL_PRIM void HL_NAME(release)() {
    Discord_Shutdown();
}

#define IMPLEMENT_PRESENCE_PROPERTY_UPDATE(paramName, type)     \
void update_##paramName(type _value, bool _autoSend) {          \
    if( !init ) return;                                         \
    presence.##paramName = _value;                              \
    if( _autoSend ){                                            \
        Discord_UpdatePresence(&presence);                      \
    }                                                           \
}                                                               \

#define IMPLEMENT_PRESENCE_STRING_PROPERTY_UPDATE(paramName)                \
IMPLEMENT_PRESENCE_PROPERTY_UPDATE(paramName, const char*)                  \
HL_PRIM void HL_NAME(update_##paramName)(vbyte* _value, bool _autoSend){    \
    static char paramName[128];                                             \
    strcpy_s(paramName, 128, (const char*) _value);                         \
    update_##paramName(paramName, _autoSend);                               \
}                                                                           \
DEFINE_PRIM(_VOID, update_##paramName, _BYTES _BOOL)

#define IMPLEMENT_PRESENCE_INTEGER_PROPERTY_UPDATE(paramName)               \
IMPLEMENT_PRESENCE_PROPERTY_UPDATE(paramName, int)                          \
HL_PRIM void HL_NAME(update_##paramName)(int _value, bool _autoSend){       \
    update_##paramName(_value, _autoSend);                                  \
}                                                                           \
DEFINE_PRIM(_VOID, update_##paramName, _I32 _BOOL)


IMPLEMENT_PRESENCE_STRING_PROPERTY_UPDATE(state)
IMPLEMENT_PRESENCE_STRING_PROPERTY_UPDATE(details)
IMPLEMENT_PRESENCE_STRING_PROPERTY_UPDATE(largeImageKey)
IMPLEMENT_PRESENCE_STRING_PROPERTY_UPDATE(largeImageText)
IMPLEMENT_PRESENCE_STRING_PROPERTY_UPDATE(smallImageKey)
IMPLEMENT_PRESENCE_STRING_PROPERTY_UPDATE(smallImageText)
IMPLEMENT_PRESENCE_STRING_PROPERTY_UPDATE(matchSecret)
IMPLEMENT_PRESENCE_STRING_PROPERTY_UPDATE(joinSecret)
IMPLEMENT_PRESENCE_STRING_PROPERTY_UPDATE(spectateSecret)

IMPLEMENT_PRESENCE_INTEGER_PROPERTY_UPDATE(partySize)
IMPLEMENT_PRESENCE_INTEGER_PROPERTY_UPDATE(partyMax)
IMPLEMENT_PRESENCE_INTEGER_PROPERTY_UPDATE(startTimestamp)
IMPLEMENT_PRESENCE_INTEGER_PROPERTY_UPDATE(endTimestamp)

DEFINE_PRIM(_VOID, init, _BYTES _BYTES)
DEFINE_PRIM(_VOID, release, _NO_ARG)
