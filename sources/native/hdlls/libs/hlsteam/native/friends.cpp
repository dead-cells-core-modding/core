#include "steamwrap.h"

vdynamic *CallbackHandler::EncodePersonaChange( PersonaStateChange_t *d ) {
	HLValue ret;
	ret.Set("user",d->m_ulSteamID);
	ret.Set("flags", d->m_nChangeFlags);
	return ret.value;
}

HL_PRIM vbyte *HL_NAME(get_user_name)( vuid uid ) {
	if( !SteamFriends() )
		return (vbyte*)"Unknown";
	return (vbyte*)SteamFriends()->GetFriendPersonaName(hl_to_uid(uid));
}

HL_PRIM bool HL_NAME(request_user_information)( vuid uid, bool nameOnly ) {
	return SteamFriends()->RequestUserInformation(hl_to_uid(uid), nameOnly);
}

HL_PRIM vbyte *HL_NAME(get_user_avatar)( vuid uid, int size, uint32 *width, uint32 *height ) {
	int img = 0;
	vbyte *out;
	switch( size ) {
	case 0: img = SteamFriends()->GetSmallFriendAvatar(hl_to_uid(uid)); break;
	case 1: img = SteamFriends()->GetMediumFriendAvatar(hl_to_uid(uid)); break;
	case 2: img = SteamFriends()->GetLargeFriendAvatar(hl_to_uid(uid)); break;
	}
	if( img == 0 )
		return NULL;
	if( !SteamUtils()->GetImageSize(img,width,height) )
		return NULL;
	size = *width * *height * 4;
	out = (vbyte*)hl_gc_alloc_noptr(size);
	if( !SteamUtils()->GetImageRGBA(img,out,size) )
		return NULL;
	return out;
}

HL_PRIM varray *HL_NAME(get_friends)( int flags ) {
	int count = SteamFriends()->GetFriendCount(flags);
	varray *a = hl_alloc_array(&hlt_bytes,count);
	int i;
	for(i=0;i<count;i++)
		hl_aptr(a,vbyte*)[i] = hl_of_uid(SteamFriends()->GetFriendByIndex(i,flags));
	return a;
}

HL_PRIM bool HL_NAME(has_friend)( vuid uid, int flags ) {
	return SteamFriends()->HasFriend(hl_to_uid(uid),flags);
}

HL_PRIM void HL_NAME(activate_overlay_user)( vbyte *dialog, vuid uid ) {
	if( uid == NULL )
		SteamFriends()->ActivateGameOverlay((char*)dialog);
	else
		SteamFriends()->ActivateGameOverlayToUser((char*)dialog,hl_to_uid(uid));
}

HL_PRIM void HL_NAME(activate_overlay_store)( int appid, int flag ) {
	SteamFriends()->ActivateGameOverlayToStore((AppId_t)appid,(EOverlayToStoreFlag)flag);
}

DEFINE_PRIM(_BYTES, get_user_name, _UID);
DEFINE_PRIM(_BYTES, get_user_avatar, _UID _I32 _REF(_I32) _REF(_I32));
DEFINE_PRIM(_BOOL, request_user_information, _UID _BOOL);
DEFINE_PRIM(_ARR, get_friends, _I32);
DEFINE_PRIM(_BOOL, has_friend, _UID _I32);
DEFINE_PRIM(_VOID, activate_overlay_user, _BYTES _UID);
DEFINE_PRIM(_VOID, activate_overlay_store, _I32 _I32);

