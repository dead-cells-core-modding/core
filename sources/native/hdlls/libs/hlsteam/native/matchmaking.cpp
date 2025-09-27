#include "steamwrap.h"

vdynamic *CallbackHandler::EncodeLobbyData( LobbyDataUpdate_t *d ) {
	if( !d->m_bSuccess ) return NULL;
	HLValue ret;
	ret.Set("lobby",d->m_ulSteamIDLobby);
	if( d->m_ulSteamIDMember != d->m_ulSteamIDLobby ) ret.Set("user",d->m_ulSteamIDMember);
	return ret.value;
}

vdynamic *CallbackHandler::EncodeLobbyChatUpdate( LobbyChatUpdate_t *d ) {
	HLValue ret;
	ret.Set("lobby",d->m_ulSteamIDLobby);
	ret.Set("user",d->m_ulSteamIDUserChanged);
	ret.Set("origin", d->m_ulSteamIDMakingChange);
	ret.Set("flags", d->m_rgfChatMemberStateChange);
	return ret.value;
}

vdynamic *CallbackHandler::EncodeLobbyChatMsg( LobbyChatMsg_t *d ) {
	HLValue ret;
	ret.Set("lobby",d->m_ulSteamIDLobby);
	ret.Set("user",d->m_ulSteamIDUser);
	ret.Set("type",d->m_eChatEntryType);
	ret.Set("cid", d->m_iChatID);
	return ret.value;
}

vdynamic *CallbackHandler::EncodeLobbyJoinRequest( GameLobbyJoinRequested_t *d ) {
	HLValue ret;
	ret.Set("lobby", d->m_steamIDLobby);
	ret.Set("user", d->m_steamIDFriend);
	return ret.value;
}

// --------- Lobby Search --------------------------

static void on_lobby_list( vclosure *c, LobbyMatchList_t *result, bool error ) {
	vdynamic d;
	d.t = &hlt_i32;
	d.v.i = error ? -1 : result->m_nLobbiesMatching;
	dyn_call_result(c,&d,error);
}

HL_PRIM CClosureCallResult<LobbyMatchList_t>* HL_NAME(request_lobby_list)( vclosure *closure ) {
	if( !CheckInit() ) return NULL;
	ASYNC_CALL(SteamMatchmaking()->RequestLobbyList(), LobbyMatchList_t, on_lobby_list);
	return m_call;
}

HL_PRIM vuid HL_NAME(get_lobby_by_index)( int index ) {
	return hl_of_uid(SteamMatchmaking()->GetLobbyByIndex(index));
}

HL_PRIM void HL_NAME(request_filter_string)( vbyte *key, vbyte *value, int type ) {
	SteamMatchmaking()->AddRequestLobbyListStringFilter((char*)key,(char*)value,(ELobbyComparison)type);
}

HL_PRIM void HL_NAME(request_filter_numerical)( vbyte *key, int value, int type ) {
	SteamMatchmaking()->AddRequestLobbyListNumericalFilter((char*)key,value,(ELobbyComparison)type);
}

HL_PRIM void HL_NAME(request_filter_near_value)( vbyte *key, int value ) {
	SteamMatchmaking()->AddRequestLobbyListNearValueFilter((char*)key,value);
}

HL_PRIM void HL_NAME(request_filter_slots_available)( int slots ) {
	SteamMatchmaking()->AddRequestLobbyListFilterSlotsAvailable(slots);
}

HL_PRIM void HL_NAME(request_filter_distance)( int distance ) {
	SteamMatchmaking()->AddRequestLobbyListDistanceFilter((ELobbyDistanceFilter)distance);
}

HL_PRIM void HL_NAME(request_result_count)( int max ) {
	SteamMatchmaking()->AddRequestLobbyListResultCountFilter(max);
}

DEFINE_PRIM(_CRESULT, request_lobby_list, _CALLB(_I32));
DEFINE_PRIM(_UID, get_lobby_by_index, _I32);

DEFINE_PRIM(_VOID, request_filter_string, _BYTES _BYTES _I32);
DEFINE_PRIM(_VOID, request_filter_numerical, _BYTES _I32 _I32);
DEFINE_PRIM(_VOID, request_filter_near_value, _BYTES _I32);
DEFINE_PRIM(_VOID, request_filter_slots_available, _I32);
DEFINE_PRIM(_VOID, request_filter_distance, _I32);
DEFINE_PRIM(_VOID, request_result_count, _I32);

// --------- Lobby Create / Manage --------------------------

static void on_lobby_created( vclosure *c, LobbyCreated_t *result, bool error ) {
	error = error || result->m_eResult != k_EResultOK;
	vdynamic d;
	hl_set_uid(&d,error ? 0 : result->m_ulSteamIDLobby);
	dyn_call_result(c,&d,error);
}

HL_PRIM CClosureCallResult<LobbyCreated_t>* HL_NAME(create_lobby)( ELobbyType lobbyType, int maxMembers, vclosure *closure ) {
	ASYNC_CALL(SteamMatchmaking()->CreateLobby(lobbyType,maxMembers), LobbyCreated_t, on_lobby_created);
	return m_call;
}

HL_PRIM void HL_NAME(leave_lobby)( vuid uid ) {
	SteamMatchmaking()->LeaveLobby(hl_to_uid(uid));
}

static void on_lobby_joined( vclosure *c, LobbyEnter_t *result, bool error ) {
	vdynamic d;
	d.t = &hlt_bool;
	d.v.b = result->m_bLocked;
	dyn_call_result(c,&d,error);
}

HL_PRIM CClosureCallResult<LobbyEnter_t>* HL_NAME(join_lobby)( vuid uid, vclosure *closure ) {
	ASYNC_CALL(SteamMatchmaking()->JoinLobby(hl_to_uid(uid)), LobbyEnter_t, on_lobby_joined);
	return m_call;
}

HL_PRIM int HL_NAME(get_num_lobby_members)( vuid uid ) {
	return SteamMatchmaking()->GetNumLobbyMembers(hl_to_uid(uid));
}

HL_PRIM vuid HL_NAME(get_lobby_member_by_index)( vuid uid, int index ) {
	return hl_of_uid(SteamMatchmaking()->GetLobbyMemberByIndex(hl_to_uid(uid),index));
}

HL_PRIM vuid HL_NAME(get_lobby_owner)( vuid uid ) {
	return hl_of_uid(SteamMatchmaking()->GetLobbyOwner(hl_to_uid(uid)));
}

HL_PRIM void HL_NAME(lobby_invite_friends)( vuid uid ) {
	SteamFriends()->ActivateGameOverlayInviteDialog(hl_to_uid(uid));
}

HL_PRIM bool HL_NAME(send_lobby_chat_msg)( vuid uid, vbyte *msg, int len ) {
	return SteamMatchmaking()->SendLobbyChatMsg(hl_to_uid(uid),msg,len);
}

HL_PRIM vuid HL_NAME(get_lobby_chat_entry)( vuid uid, int cid, vbyte *data, int maxData, int *type, int *outLen ) {
	CSteamID user;
	EChatEntryType chatType;
	*outLen = SteamMatchmaking()->GetLobbyChatEntry(hl_to_uid(uid), cid, &user, data, maxData, &chatType);
	*type = chatType;
	return hl_of_uid(user);
}

HL_PRIM bool HL_NAME(lobby_invite_user)( vuid lid, vuid fid ) {
	return SteamMatchmaking()->InviteUserToLobby(hl_to_uid(lid),hl_to_uid(fid));
}

HL_PRIM int HL_NAME(get_lobby_member_limit)( vuid uid ) {
	return SteamMatchmaking()->GetLobbyMemberLimit(hl_to_uid(uid));
}

HL_PRIM void HL_NAME(set_lobby_member_limit)( vuid uid, int count ) {
	SteamMatchmaking()->SetLobbyMemberLimit(hl_to_uid(uid), count);
}

DEFINE_PRIM(_CRESULT, create_lobby, _I32 _I32 _CALLB(_UID));
DEFINE_PRIM(_VOID, leave_lobby, _UID);
DEFINE_PRIM(_CRESULT, join_lobby, _UID _CALLB(_BOOL));

DEFINE_PRIM(_I32, get_num_lobby_members, _UID);
DEFINE_PRIM(_UID, get_lobby_member_by_index, _UID _I32);
DEFINE_PRIM(_I32, get_lobby_member_limit, _UID);
DEFINE_PRIM(_VOID, set_lobby_member_limit, _UID _I32);

DEFINE_PRIM(_UID, get_lobby_owner, _UID);
DEFINE_PRIM(_VOID, lobby_invite_friends, _UID);
DEFINE_PRIM(_BOOL, lobby_invite_user, _UID _UID);
DEFINE_PRIM(_BOOL, send_lobby_chat_msg, _UID _BYTES _I32);
DEFINE_PRIM(_UID, get_lobby_chat_entry, _UID _I32 _BYTES _I32 _REF(_I32) _REF(_I32));

// --------- Lobby Data --------------------------

HL_PRIM bool HL_NAME(request_lobby_data)( vuid uid ) {
	return SteamMatchmaking()->RequestLobbyData(hl_to_uid(uid));
}

HL_PRIM const char *HL_NAME(get_lobby_data)( vuid uid, const char *key ) {
	return SteamMatchmaking()->GetLobbyData(hl_to_uid(uid),key);
}

HL_PRIM bool HL_NAME(set_lobby_data)( vuid uid, const char *key, const char *value ) {
	return SteamMatchmaking()->SetLobbyData(hl_to_uid(uid), key, value);
}

HL_PRIM bool HL_NAME(delete_lobby_data)( vuid uid, const char *key ) {
	return SteamMatchmaking()->DeleteLobbyData(hl_to_uid(uid),key);
}

HL_PRIM int HL_NAME(get_lobby_data_count)( vuid uid ) {
	return SteamMatchmaking()->GetLobbyDataCount(hl_to_uid(uid));
}

HL_PRIM bool HL_NAME(get_lobby_data_byindex)( vuid uid, int index, char *key, int ksize, char *value, int vsize ) {
	return SteamMatchmaking()->GetLobbyDataByIndex(hl_to_uid(uid), index, key, ksize, value, vsize);
}

HL_PRIM vbyte *HL_NAME(get_lobby_member_data)( vuid lid, vuid uid, const char *key ) {
	return (vbyte*)SteamMatchmaking()->GetLobbyMemberData(hl_to_uid(lid), hl_to_uid(uid), key );
}

HL_PRIM void HL_NAME(set_lobby_member_data)( vuid lid, const char *key, const char *value ) {
	SteamMatchmaking()->SetLobbyMemberData(hl_to_uid(lid), key, value);
}

DEFINE_PRIM(_BOOL, request_lobby_data, _UID);
DEFINE_PRIM(_BYTES, get_lobby_data, _UID _BYTES);
DEFINE_PRIM(_BOOL, set_lobby_data, _UID _BYTES _BYTES);
DEFINE_PRIM(_BOOL, delete_lobby_data, _UID _BYTES);
DEFINE_PRIM(_I32, get_lobby_data_count, _UID);
DEFINE_PRIM(_BOOL, get_lobby_data_byindex, _UID _I32 _BYTES _I32 _BYTES _I32);
DEFINE_PRIM(_BYTES, get_lobby_member_data, _UID _UID _BYTES);
DEFINE_PRIM(_VOID, set_lobby_member_data, _UID _BYTES _BYTES);

// ---------
