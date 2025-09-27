#define HL_NAME(n) steam_##n
#include <hl.h>

#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <vector>
#include <sstream>
#include <iostream>
#include <map>

#include <steam/steam_api.h>
#include <steam/steam_gameserver.h>
#include <steam/isteamappticket.h>

typedef vbyte *		vuid;
#define _UID		_BYTES
#define hlt_uid		hlt_bytes

void dyn_call_result( vclosure *c, vdynamic *p, bool error );
void hl_set_uid( vdynamic *out, int64 uid );
CSteamID hl_to_uid( vuid v );
vuid hl_of_uid( CSteamID id );
uint64 hl_to_uint64(vuid v);
vuid hl_of_uint64(uint64 id);

template< class T >
class CClosureCallResult : public CCallResult<CClosureCallResult<T>,T> {
	vclosure *closure;
	void (*on_result)( vclosure *, T *, bool);
public:
	CClosureCallResult( vclosure *cval, void (*on_result)( vclosure *t, T*, bool) ) {
		this->closure = cval;
		this->on_result = on_result;
		hl_add_root(&closure);
	}
	~CClosureCallResult() {
		hl_remove_root(&closure);
	}
	void OnResult( T *result, bool onIOError ) {
		on_result(closure,result,onIOError);
		delete this;}
};

#define ASYNC_CALL(steam_call, type, on_result) \
	CClosureCallResult<type> *m_call = new CClosureCallResult<type>(closure,on_result); \
	m_call->Set(steam_call, m_call, &CClosureCallResult<type>::OnResult);

#define _CRESULT	_ABSTRACT(steam_call_result)
#define _CALLB(T)	_FUN(_VOID, T _BOOL)

typedef enum {
	None,
	GamepadTextInputDismissed,
	UserStatsReceived,
	UserStatsStored,
	UserAchievementStored,
	LeaderboardFound,
	ScoreUploaded,
	ScoreDownloaded,
	GlobalStatsReceived,
} event_type;

class CallbackHandler {
private:
	//SteamLeaderboard_t m_curLeaderboard;
	std::map<std::string, SteamLeaderboard_t> m_leaderboards;
public:

	CallbackHandler() :
#	define EVENT_DECL(name,type) m_##name(this,&CallbackHandler::On##name),
#	include "events.h"
		m_leaderboards()
	{}

#	define EVENT_DECL(name,type) STEAM_CALLBACK(CallbackHandler, On##name, type, m_##name); vdynamic *Encode##name( type *t );
#	include "events.h"

#	define EVENT_IMPL(name,type) vdynamic *CallbackHandler::Encode##name( type *d )

	void FindLeaderboard(const char* name);
	void OnLeaderboardFound( LeaderboardFindResult_t *pResult, bool bIOFailure);
	CCallResult<CallbackHandler, LeaderboardFindResult_t> m_callResultFindLeaderboard;

	bool UploadScore(const std::string& leaderboardId, int score, int detail);
	void OnScoreUploaded( LeaderboardScoreUploaded_t *pResult, bool bIOFailure);
	CCallResult<CallbackHandler, LeaderboardScoreUploaded_t> m_callResultUploadScore;

	bool DownloadScores(const std::string& leaderboardId, int numBefore, int numAfter);
	void OnScoreDownloaded( LeaderboardScoresDownloaded_t *pResult, bool bIOFailure);
	CCallResult<CallbackHandler, LeaderboardScoresDownloaded_t> m_callResultDownloadScore;

	void RequestGlobalStats();
	void OnGlobalStatsReceived(GlobalStatsReceived_t* pResult, bool bIOFailure);
	CCallResult<CallbackHandler, GlobalStatsReceived_t> m_callResultRequestGlobalStats;

};

class HLValue {
public:
	vdynamic *value;
	HLValue() {
		value = (vdynamic*)hl_alloc_dynobj();
	}
	void Set( const char *name, CSteamID uid ) {
		hl_dyn_setp(value, hl_hash_utf8(name), &hlt_uid, hl_of_uid(uid));
	}
	void Set( const char *name, uint64 uid ) {
		hl_dyn_setp(value, hl_hash_utf8(name), &hlt_uid, hl_of_uint64(uid));
	}
	void Set( const char *name, uint32 v ) {
		Set(name,(int)v);
	}
	void Set( const char *name, bool b ) {
		hl_dyn_seti(value, hl_hash_utf8(name), &hlt_bool, b);
	}
	void Set( const char *name, int v ) {
		hl_dyn_seti(value, hl_hash_utf8(name), &hlt_i32, v);
	}
	void Set(const char *name, double v) {
		hl_dyn_setd(value, hl_hash_utf8(name), v);
	}
	void Set(const char *name, float v) {
		hl_dyn_setf(value, hl_hash_utf8(name), v);
	}
	void Set( const char *name, const char *b ) {
		hl_dyn_setp(value, hl_hash_utf8(name), &hlt_bytes, hl_copy_bytes((vbyte*)b, (int)strlen(b)+1));
	}
	void Set( const char *name, vdynamic *d ) {
		hl_dyn_setp(value, hl_hash_utf8(name), &hlt_dyn, d);
	}
};

extern CallbackHandler *s_callbackHandler;

void SendEvent(event_type type, bool success, const char *data);
bool CheckInit();

SteamParamStringArray_t * getSteamParamStringArray(const char * str);
void deleteSteamParamStringArray(SteamParamStringArray_t * params);
void split(const std::string &s, char delim, std::vector<std::string> &elems);
