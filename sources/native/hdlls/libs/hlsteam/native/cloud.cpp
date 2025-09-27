#include "steamwrap.h"

HL_PRIM int HL_NAME(get_file_count)(){
	if (!CheckInit()) return -1;
	int fileCount = SteamRemoteStorage()->GetFileCount();
	return fileCount;
}

HL_PRIM int HL_NAME(get_file_size)(vbyte *fileName){
	if (!CheckInit()) return -1;
	int fileSize = SteamRemoteStorage()->GetFileSize((char*)fileName);
	return fileSize;
}

HL_PRIM bool HL_NAME(file_exists)(vbyte *fileName){
	if (!CheckInit()) return false;
	return SteamRemoteStorage()->FileExists((char*)fileName);
}

HL_PRIM vbyte *HL_NAME(file_read)(vbyte *fileName, int *len){
	if (!CheckInit()) return NULL;

	const char * fName = (char*)fileName;

	bool exists = SteamRemoteStorage()->FileExists(fName);
	if(!exists) return NULL;

	int length = SteamRemoteStorage()->GetFileSize(fName);

	char *bytesData = (char *)hl_gc_alloc_noptr(length);
	int32 result = SteamRemoteStorage()->FileRead(fName, bytesData, length);

	*len = length;
	return (vbyte*)bytesData;
}

HL_PRIM bool HL_NAME(file_write)(vbyte *fileName, vbyte *bytes, int length){
	if (!CheckInit()) return false;
	if (length <= 0) return false;
	return SteamRemoteStorage()->FileWrite((char*)fileName, (char*)bytes, length);
}

HL_PRIM bool HL_NAME(file_delete)(vbyte *fileName){
	if (!CheckInit()) return false;
	return SteamRemoteStorage()->FileDelete((char*)fileName);
}

static void on_file_shared(vclosure *c, RemoteStorageFileShareResult_t *result, bool error) {
	if (!error && result->m_eResult == k_EResultOK) {
		vdynamic d;
		hl_set_uid(&d, result->m_hFile);
		dyn_call_result(c, &d, error);
	}else {
		dyn_call_result(c, NULL, true);
	}
}

HL_PRIM CClosureCallResult<RemoteStorageFileShareResult_t>* HL_NAME(file_share)(vbyte *fileName, vclosure *closure) {
	if (!CheckInit()) return NULL;
	ASYNC_CALL(SteamRemoteStorage()->FileShare((const char*)fileName), RemoteStorageFileShareResult_t, on_file_shared);
	return m_call;
}

HL_PRIM bool HL_NAME(is_cloud_enabled_for_account)() {
	if (!CheckInit()) return false;
	return SteamRemoteStorage()->IsCloudEnabledForAccount();
}

HL_PRIM bool HL_NAME(is_cloud_enabled_for_app)(){
	if (!CheckInit()) return false;
	return SteamRemoteStorage()->IsCloudEnabledForApp();
}

HL_PRIM void HL_NAME(set_cloud_enabled_for_app)(bool enabled){
	if (!CheckInit()) return;
	SteamRemoteStorage()->SetCloudEnabledForApp(enabled);
}

HL_PRIM void HL_NAME(get_quota)( double *total, double *available ){
	if (!CheckInit()) return;

	uint64 iTotal = 0;
	uint64 iAvailable = 0;

	SteamRemoteStorage()->GetQuota(&iTotal, &iAvailable);

	*total = (double)iTotal;
	*available = (double)iAvailable;
}

DEFINE_PRIM(_I32, get_file_count, _NO_ARG);
DEFINE_PRIM(_I32, get_file_size, _BYTES);
DEFINE_PRIM(_BOOL, file_exists, _BYTES);
DEFINE_PRIM(_BYTES, file_read, _BYTES _REF(_I32));
DEFINE_PRIM(_BOOL, file_write, _BYTES _BYTES _I32);
DEFINE_PRIM(_BOOL, file_delete, _BYTES);
DEFINE_PRIM(_CRESULT, file_share, _BYTES _CALLB(_UID));
DEFINE_PRIM(_BOOL, is_cloud_enabled_for_app, _NO_ARG);
DEFINE_PRIM(_BOOL, is_cloud_enabled_for_account, _NO_ARG);
DEFINE_PRIM(_VOID, set_cloud_enabled_for_app, _BOOL);
DEFINE_PRIM(_VOID, get_quota, _NO_ARG _REF(_F64) _REF(_F64));