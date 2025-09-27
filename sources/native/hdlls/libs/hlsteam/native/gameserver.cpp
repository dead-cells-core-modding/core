#include "steamwrap.h"

static void GlobalEvent( int id, vdynamic *v );

class GameServerHandler {
	std::map<std::string, void*> m;
public:
	GameServerHandler() :
#	define EVENT_DECL(name,type) m_##name(this,&GameServerHandler::On##name),
#	include "serverevents.h"
		m()
	{}

#	define EVENT_DECL(name,type) STEAM_GAMESERVER_CALLBACK(GameServerHandler, On##name, type, m_##name); vdynamic *Encode##name( type *t );
#	include "serverevents.h"

#	undef EVENT_IMPL
#	define EVENT_IMPL(name,type) vdynamic *GameServerHandler::Encode##name( type *d )
};

#define EVENT_DECL(name,type) void GameServerHandler::On##name( type *t ) { GlobalEvent(type::k_iCallback, Encode##name(t)); }
#include "serverevents.h"

EVENT_IMPL(ServerConnectFailure, SteamServerConnectFailure_t) {
	HLValue v;
	v.Set("result", d->m_eResult);
	v.Set("stillRetrying", d->m_bStillRetrying);
	return v.value;
}

EVENT_IMPL(ServersConnected, SteamServersConnected_t) {
	return NULL;
}

EVENT_IMPL(ServersDisconnected, SteamServersDisconnected_t) {
	HLValue v;
	v.Set("result", d->m_eResult);
	return v.value;
}

EVENT_IMPL(P2PSessionRequest, P2PSessionRequest_t) {
	HLValue v;
	v.Set("uid", d->m_steamIDRemote);
	return v.value;
}

EVENT_IMPL(P2PSessionConnectFail, P2PSessionConnectFail_t) {
	HLValue v;
	v.Set("uid", d->m_steamIDRemote);
	v.Set("error", d->m_eP2PSessionError);
	return v.value;
}

EVENT_IMPL(PolicyResponse, GSPolicyResponse_t) {
	HLValue v;
	v.Set("secure", d->m_bSecure);
	return v.value;
}

EVENT_IMPL(ValidateAuthTicketResponse, ValidateAuthTicketResponse_t ) {
	HLValue v;
	v.Set("steamId", d->m_SteamID);
	v.Set("ownerSteamId", d->m_OwnerSteamID);
	v.Set("authSessionResponse",d->m_eAuthSessionResponse);
	return v.value;
}

static GameServerHandler *serverHandler = NULL;
static vclosure *globalEvent = NULL;

static void GlobalEvent( int id, vdynamic *v ) {
	vdynamic i;
	vdynamic *args[2];
	i.t = &hlt_i32;
	i.v.i = id;
	args[0] = &i;
	args[1] = v;
	hl_dyn_call(globalEvent, args, 2);
}

HL_PRIM void HL_NAME(gameserver_setup)( vclosure *onGlobalEvent ){
	serverHandler = new GameServerHandler();
	globalEvent = onGlobalEvent;
	hl_add_root(&globalEvent);
}

bool HL_NAME(gameserver_init)( int ip, int port, int gameport, int queryport, int serverMode, char *version ) {
	return SteamGameServer_Init(ip,gameport,queryport,(EServerMode)serverMode, version);
}

void HL_NAME(gameserver_runcallbacks)() {
	SteamGameServer_RunCallbacks();
}

void HL_NAME(gameserver_shutdown)() {
	SteamGameServer_Shutdown();
}

void HL_NAME(gameserver_logon_anonymous)() {
	SteamGameServer()->LogOnAnonymous();
}

void HL_NAME(gameserver_enable_heartbeats)( bool b ) {
	SteamGameServer()->SetAdvertiseServerActive(b);
}

void HL_NAME(gameserver_config)( char *modDir, char *product, char *desc ) {
	SteamGameServer()->SetModDir(modDir);
	SteamGameServer()->SetProduct(product);
	SteamGameServer()->SetGameDescription(desc);
	SteamGameServer()->SetDedicatedServer(true);
}

void HL_NAME(gameserver_info)( int maxPlayers, bool password, char *serverName, int botCount, char *mapName ) {
	SteamGameServer()->SetMaxPlayerCount(maxPlayers);
	SteamGameServer()->SetPasswordProtected(password);
	SteamGameServer()->SetServerName(serverName);
	SteamGameServer()->SetBotPlayerCount(botCount);
	SteamGameServer()->SetMapName(mapName);
}

vuid HL_NAME(gameserver_get_steam_id)() {
	return hl_of_uid(SteamGameServer()->GetSteamID());
}

int HL_NAME(gameserver_get_public_ip)() {
	return (int)SteamGameServer()->GetPublicIP().m_unIPv4;
}

DEFINE_PRIM(_BOOL, gameserver_init, _I32 _I32 _I32 _I32 _I32 _BYTES);
DEFINE_PRIM(_VOID, gameserver_setup, _FUN(_VOID, _I32 _DYN));
DEFINE_PRIM(_VOID, gameserver_runcallbacks, _NO_ARG);
DEFINE_PRIM(_VOID, gameserver_shutdown, _NO_ARG);
DEFINE_PRIM(_VOID, gameserver_logon_anonymous, _NO_ARG);
DEFINE_PRIM(_VOID, gameserver_enable_heartbeats, _BOOL);
DEFINE_PRIM(_VOID, gameserver_config, _BYTES _BYTES _BYTES);
DEFINE_PRIM(_VOID, gameserver_info, _I32 _BOOL _BYTES _I32 _BYTES);
DEFINE_PRIM(_UID, gameserver_get_steam_id, _NO_ARG);
DEFINE_PRIM(_I32, gameserver_get_public_ip, _NO_ARG);

// --------- list --------------------------

class HLServerResponse : public ISteamMatchmakingServerListResponse {
public:

	vclosure *callb;

	HLServerResponse( vclosure *callb ) {
		this->callb = callb;
		hl_add_root(&this->callb);
	}

	~HLServerResponse() {
		hl_remove_root(&this->callb);
	}

	void ServerResponded( HServerListRequest hRequest, int iServer ) {
		saveServer(hRequest,iServer);
	}

	void ServerFailedToRespond( HServerListRequest hRequest, int iServer ) {
		saveServer(hRequest,iServer);
	}

	void saveServer( HServerListRequest hRequest, int iServer ) {
		gameserveritem_t *details = SteamMatchmakingServers()->GetServerDetails(hRequest,iServer);
		HLValue v;
		v.Set("ip", details->m_NetAdr.GetIP());
		v.Set("port", details->m_NetAdr.GetQueryPort());
		v.Set("ping", details->m_bHadSuccessfulResponse ? details->m_nPing : -1);
		v.Set("id", details->m_steamID);
		hl_dyn_call(callb, &v.value, 1);
		//printf("SERVER DIDN'T ANSWER %d.%d.%d.%d:%d\n",(ip >> 24),(ip >> 16) & 0xFF, (ip >> 8) & 0xFF, ip & 0xFF, );
	}

	void RefreshComplete( HServerListRequest hRequest, EMatchMakingServerResponse response ) {
		vdynamic *arg = NULL;
		hl_dyn_call(callb, &arg, 1);
		//printf("COMPLETE %d\n", response);
		delete this;
	}
};

HL_PRIM void HL_NAME(request_internet_server_list)( int appId, varray *filters, vclosure *callb ) {
	int nfilters = filters ? filters->size >> 1 : 0;
	HLServerResponse *api = new HLServerResponse(callb);
	MatchMakingKeyValuePair_t *tfilters = new MatchMakingKeyValuePair_t[nfilters];
	int i;
	for(i=0;i<nfilters;i++) {
		strcpy(tfilters[i].m_szKey, hl_aptr(filters,char*)[i<<1]);
		strcpy(tfilters[i].m_szValue, hl_aptr(filters,char*)[(i<<1)+1]);
	}
	SteamMatchmakingServers()->RequestInternetServerList(appId,&tfilters,nfilters,api);
	delete tfilters;
}

DEFINE_PRIM(_VOID, request_internet_server_list, _I32 _ARR _FUN(_VOID, _DYN));

// ---------