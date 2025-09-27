#include "steamwrap.h"

#define Networking()	(SteamNetworking() ? SteamNetworking() : SteamGameServerNetworking())

vdynamic *CallbackHandler::EncodeP2PSessionRequest( P2PSessionRequest_t *d ) {
	HLValue v;
	v.Set("uid", d->m_steamIDRemote);
	return v.value;
}

vdynamic *CallbackHandler::EncodeP2PSessionConnectionFail( P2PSessionConnectFail_t *d ) {
	HLValue v;
	v.Set("uid", d->m_steamIDRemote);
	v.Set("error", d->m_eP2PSessionError);
	return v.value;
}

HL_PRIM bool HL_NAME(send_p2p_packet)( vuid uid, vbyte *data, int length, int type, int channel ) {
	return Networking()->SendP2PPacket(hl_to_uid(uid),data,length,(EP2PSend)type,channel);
}

HL_PRIM bool HL_NAME(accept_p2p_session)( vuid uid ) {
	return Networking()->AcceptP2PSessionWithUser(hl_to_uid(uid));
}

HL_PRIM bool HL_NAME(is_p2p_packet_available)( uint32 *msgSize, int channel ) {
	return Networking()->IsP2PPacketAvailable(msgSize,channel);
}

HL_PRIM vuid HL_NAME(read_p2p_packet)( vbyte *data, int maxLength, uint32 *length, int channel ) {
	CSteamID uid;
	if( !Networking()->ReadP2PPacket(data, maxLength, length, &uid, channel) )
		return NULL;
	return hl_of_uid(uid);
}

HL_PRIM vdynamic *HL_NAME(get_p2p_session_data)( vuid uid ) {
	P2PSessionState_t state;
	if( !Networking()->GetP2PSessionState(hl_to_uid(uid),&state) )
		return NULL;
	HLValue v;
	v.Set("connecting",state.m_bConnecting != 0);
	v.Set("alive",state.m_bConnectionActive != 0);
	v.Set("relay",state.m_bUsingRelay != 0);
	v.Set("error",state.m_eP2PSessionError);
	v.Set("bytesSendQueue", state.m_nBytesQueuedForSend);
	v.Set("packetsSendQueue", state.m_nPacketsQueuedForSend);
	v.Set("remoteIP", state.m_nRemoteIP);
	v.Set("remotePort", state.m_nRemotePort);
	return v.value;
}

HL_PRIM bool HL_NAME(close_p2p_session)( vuid uid ) {
	return Networking()->CloseP2PSessionWithUser(hl_to_uid(uid));
}

DEFINE_PRIM(_BOOL, send_p2p_packet, _UID _BYTES _I32 _I32 _I32);
DEFINE_PRIM(_BOOL, accept_p2p_session, _UID);
DEFINE_PRIM(_BOOL, is_p2p_packet_available, _REF(_I32) _I32);
DEFINE_PRIM(_UID, read_p2p_packet, _BYTES _I32 _REF(_I32) _I32);
DEFINE_PRIM(_DYN, get_p2p_session_data, _UID);
DEFINE_PRIM(_BOOL, close_p2p_session, _UID);
