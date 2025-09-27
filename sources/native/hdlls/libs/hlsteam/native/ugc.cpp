#include "steamwrap.h"

vdynamic *CallbackHandler::EncodeDownloadItem(DownloadItemResult_t *d) {
	HLValue v;
	v.Set("file", d->m_nPublishedFileId);
	return v.value;
}

vdynamic *CallbackHandler::EncodeItemInstalled(ItemInstalled_t *d) {
	HLValue v;
	v.Set("file", d->m_nPublishedFileId);
	return v.value;
}

HL_PRIM varray *HL_NAME(get_subscribed_items)(){
	if (!CheckInit()) return NULL;

	int numSubscribed = SteamUGC()->GetNumSubscribedItems();
	if(numSubscribed <= 0) return hl_alloc_array(&hlt_bytes, 0);
	PublishedFileId_t* pvecPublishedFileID = new PublishedFileId_t[numSubscribed];

	int result = SteamUGC()->GetSubscribedItems(pvecPublishedFileID, numSubscribed);
	if( result <= 0 ) {
		delete[] pvecPublishedFileID;
		return hl_alloc_array(&hlt_bytes, 0);
	}

	varray *a = hl_alloc_array(&hlt_bytes, result);
	vuid *aa = hl_aptr(a, vuid);
	
	for (int i = 0; i < result; i++)
		aa[i] = hl_of_uint64(pvecPublishedFileID[i]);
	delete[] pvecPublishedFileID;
	return a;
}

HL_PRIM int HL_NAME(get_item_state)(vuid publishedFileID){
	if (!CheckInit() || publishedFileID == NULL) return 0;
	PublishedFileId_t nPublishedFileID = (PublishedFileId_t)hl_to_uint64(publishedFileID);
	return SteamUGC()->GetItemState(nPublishedFileID);
}

HL_PRIM bool HL_NAME(get_item_download_info)(vuid publishedFileID, double *downloaded, double *total ){
	if (!CheckInit() || publishedFileID == NULL) return false;

	PublishedFileId_t nPublishedFileID = (PublishedFileId_t)hl_to_uint64(publishedFileID);

	uint64 punBytesDownloaded;
	uint64 punBytesTotal;

	bool result = SteamUGC()->GetItemDownloadInfo(nPublishedFileID, &punBytesDownloaded, &punBytesTotal);

	if (!result)
		return false;

	*downloaded = (double)punBytesDownloaded;
	*total = (double)punBytesTotal;

	return result;
}

HL_PRIM bool HL_NAME(download_item)(vuid publishedFileID, bool highPriority){
	if (!CheckInit() || publishedFileID == NULL) return false;
	PublishedFileId_t nPublishedFileID = (PublishedFileId_t)hl_to_uint64(publishedFileID);

	return SteamUGC()->DownloadItem(nPublishedFileID, highPriority);
}

HL_PRIM vdynamic *HL_NAME(get_item_install_info)(vuid publishedFileID){
	if (!CheckInit() || publishedFileID == NULL) return NULL;

	PublishedFileId_t nPublishedFileID = (PublishedFileId_t)hl_to_uint64(publishedFileID);

	uint64 punSizeOnDisk;
	uint32 punTimeStamp;
	uint32 cchFolderSize = 256;
	char * pchFolder = (char *)hl_gc_alloc_noptr(cchFolderSize);

	bool result = SteamUGC()->GetItemInstallInfo(nPublishedFileID, &punSizeOnDisk, pchFolder, cchFolderSize, &punTimeStamp);

	if(result){
		HLValue ret;
		ret.Set("size", punSizeOnDisk);
		ret.Set("time", punTimeStamp);
		ret.Set("path", pchFolder);
		return ret.value;
	}

	return NULL;
}

static void on_item_subscribed(vclosure *c, RemoteStorageSubscribePublishedFileResult_t *result, bool error) {
	if (!error && result->m_eResult == k_EResultOK) {
		vdynamic d;
		hl_set_uid(&d, result->m_nPublishedFileId);
		dyn_call_result(c, &d, error);
	}
	else {
		dyn_call_result(c, NULL, true);
	}
}
HL_PRIM CClosureCallResult<RemoteStorageSubscribePublishedFileResult_t>* HL_NAME(subscribe_item)(vuid publishedFileID, vclosure *closure) {
	if (!CheckInit() || publishedFileID == NULL) return NULL;
	ASYNC_CALL(SteamUGC()->SubscribeItem(hl_to_uint64(publishedFileID)), RemoteStorageSubscribePublishedFileResult_t, on_item_subscribed);
	return m_call;
}
HL_PRIM CClosureCallResult<RemoteStorageSubscribePublishedFileResult_t>* HL_NAME(unsubscribe_item)(vuid publishedFileID, vclosure *closure) {
	if (!CheckInit() || publishedFileID == NULL) return NULL;
	ASYNC_CALL(SteamUGC()->UnsubscribeItem(hl_to_uint64(publishedFileID)), RemoteStorageSubscribePublishedFileResult_t, on_item_subscribed);
	return m_call;
}

static void on_item_deleted(vclosure *c, DeleteItemResult_t *result, bool error) {
	vdynamic d;
	d.t = &hlt_i32;
	d.v.i = result->m_eResult;
	dyn_call_result(c, &d, error);
}

HL_PRIM CClosureCallResult<DeleteItemResult_t>* HL_NAME(delete_item)(vuid updateHandle, vclosure *closure){
	if (!CheckInit() || updateHandle == NULL) return NULL;
	ASYNC_CALL(SteamUGC()->DeleteItem(hl_to_uint64(updateHandle)), DeleteItemResult_t, on_item_deleted);
	return m_call;
}

static void on_app_added(vclosure *c, AddAppDependencyResult_t *result, bool error) {
	vdynamic d;
	d.t = &hlt_i32;
	d.v.i = result->m_eResult;
	dyn_call_result(c, &d, error);
}

HL_PRIM CClosureCallResult<AddAppDependencyResult_t>* HL_NAME(add_app_dependency)(vuid updateHandle, int appId, vclosure *closure){
	if (!CheckInit() || updateHandle == NULL) return NULL;
	ASYNC_CALL(SteamUGC()->AddAppDependency(hl_to_uint64(updateHandle), appId), AddAppDependencyResult_t, on_app_added);
	return m_call;
}

static void on_app_removed(vclosure *c, RemoveAppDependencyResult_t *result, bool error) {
	vdynamic d;
	d.t = &hlt_i32;
	d.v.i = result->m_eResult;
	dyn_call_result(c, &d, error);
}

HL_PRIM CClosureCallResult<RemoveAppDependencyResult_t>* HL_NAME(remove_app_dependency)(vuid updateHandle, int appId, vclosure *closure){
	if (!CheckInit() || updateHandle == NULL) return NULL;
	ASYNC_CALL(SteamUGC()->RemoveAppDependency(hl_to_uint64(updateHandle), appId), RemoveAppDependencyResult_t, on_app_removed);
	return m_call;
}

static void on_app_dependencies(vclosure *c, GetAppDependenciesResult_t *result, bool error) {
	HLValue ret;
	int count = result->m_nNumAppDependencies;
	varray *deps = hl_alloc_array(&hlt_i32,count);
	int i;
	for(i=0;i<count;i++)
		hl_aptr(deps,int)[i] = result->m_rgAppIDs[i];
	ret.Set("result", result->m_eResult);
	ret.Set("deps", (vdynamic*)deps);
	dyn_call_result(c, ret.value, error);
}

HL_PRIM CClosureCallResult<GetAppDependenciesResult_t>* HL_NAME(get_app_dependencies)(vuid updateHandle, vclosure *closure){
	if (!CheckInit() || updateHandle == NULL) return NULL;
	ASYNC_CALL(SteamUGC()->GetAppDependencies(hl_to_uint64(updateHandle)), GetAppDependenciesResult_t, on_app_dependencies);
	return m_call;
}

DEFINE_PRIM(_ARR, get_subscribed_items, _NO_ARG);
DEFINE_PRIM(_I32, get_item_state, _UID);
DEFINE_PRIM(_BOOL, get_item_download_info, _UID _REF(_F64) _REF(_F64));
DEFINE_PRIM(_BOOL, download_item, _UID _BOOL);
DEFINE_PRIM(_DYN, get_item_install_info, _UID);
DEFINE_PRIM(_CRESULT, subscribe_item, _UID _CALLB(_UID));
DEFINE_PRIM(_CRESULT, unsubscribe_item, _UID _CALLB(_UID));
DEFINE_PRIM(_CRESULT, delete_item, _UID _CALLB(_I32));
DEFINE_PRIM(_CRESULT, add_app_dependency, _UID _I32 _CALLB(_I32));
DEFINE_PRIM(_CRESULT, remove_app_dependency, _UID _I32 _CALLB(_I32));
DEFINE_PRIM(_CRESULT, get_app_dependencies, _UID _CALLB(_DYN));

//-----------------------------------------------------------------------------------------------------------
// UGC QUERY
//-----------------------------------------------------------------------------------------------------------

HL_PRIM vuid HL_NAME(ugc_query_create_all_request)(int queryType, int matchingUGCType, int creatorAppID, int consumerAppID, int page){
	if (!CheckInit()) return NULL;

	EUGCQuery eQueryType = (EUGCQuery) queryType;
	EUGCMatchingUGCType eMatchingUGCType = (EUGCMatchingUGCType) matchingUGCType;
	AppId_t nCreatorAppID = creatorAppID;
	AppId_t nConsumerAppID = consumerAppID;

	UGCQueryHandle_t result = SteamUGC()->CreateQueryAllUGCRequest(eQueryType, eMatchingUGCType, nCreatorAppID, nConsumerAppID, page);
	return hl_of_uint64(result);
}

HL_PRIM vuid HL_NAME(ugc_query_create_user_request)(int uid, int listType, int matchingUGCType, int sortOrder, int creatorAppID, int consumerAppID, int page) {
	if (!CheckInit()) return NULL;

	EUserUGCList eListType = (EUserUGCList)listType;
	EUGCMatchingUGCType eMatchingUGCType = (EUGCMatchingUGCType)matchingUGCType;
	EUserUGCListSortOrder eSortOrder = (EUserUGCListSortOrder)sortOrder;
	AppId_t nCreatorAppID = creatorAppID;
	AppId_t nConsumerAppID = consumerAppID;
	AccountID_t accountId = uid;

	UGCQueryHandle_t result = SteamUGC()->CreateQueryUserUGCRequest(accountId, eListType, eMatchingUGCType, eSortOrder, creatorAppID, consumerAppID, page);
	return hl_of_uint64(result);
}

HL_PRIM vuid HL_NAME(ugc_query_create_details_request)(varray *fileIDs){
	if (!CheckInit() || fileIDs == NULL) return NULL;

	PublishedFileId_t * pvecPublishedFileID = new PublishedFileId_t[fileIDs->size];
	vuid *ids = hl_aptr(fileIDs, vuid);
	for (int i = 0; i < fileIDs->size; i++)
		pvecPublishedFileID[i] = ids[i] ? hl_to_uint64(ids[i]) : 0;

	UGCQueryHandle_t result = SteamUGC()->CreateQueryUGCDetailsRequest(pvecPublishedFileID, fileIDs->size);
	delete pvecPublishedFileID;
	return hl_of_uint64(result);
}

HL_PRIM bool HL_NAME(ugc_query_set_language)(vuid handle, vbyte *lang) {
	if (!CheckInit() || handle == NULL) return false;
	return SteamUGC()->SetLanguage(hl_to_uint64(handle), (char*)lang);
}

HL_PRIM bool HL_NAME(ugc_query_set_search_text)(vuid handle, vbyte *search) {
	if (!CheckInit() || handle == NULL) return false;
	return SteamUGC()->SetSearchText(hl_to_uint64(handle), (char*)search);
}

HL_PRIM bool HL_NAME(ugc_query_add_required_tag)(vuid handle, vbyte *tagName) {
	if (!CheckInit() || handle == NULL) return false;
	return SteamUGC()->AddRequiredTag(hl_to_uint64(handle), (char*)tagName);
}

HL_PRIM bool HL_NAME(ugc_query_add_required_key_value_tag)(vuid handle, vbyte *pKey, vbyte *pValue) {
	if (!CheckInit() || handle == NULL) return false;
	return SteamUGC()->AddRequiredKeyValueTag(hl_to_uint64(handle), (char*)pKey, (char*)pValue);
}

HL_PRIM bool HL_NAME(ugc_query_add_excluded_tag)(vuid handle, vbyte *tagName) {
	if (!CheckInit() || handle == NULL) return false;
	return SteamUGC()->AddExcludedTag(hl_to_uint64(handle), (char*)tagName);
}

HL_PRIM bool HL_NAME(ugc_query_set_return_metadata)(vuid handle, bool returnMetadata) {
	if (!CheckInit() || handle == NULL) return false;
	return SteamUGC()->SetReturnMetadata(hl_to_uint64(handle), returnMetadata);
}

HL_PRIM bool HL_NAME(ugc_query_set_return_key_value_tags)(vuid handle, bool returnKeyValueTags) {
	if (!CheckInit() || handle == NULL) return false;
	return SteamUGC()->SetReturnKeyValueTags(hl_to_uint64(handle), returnKeyValueTags);
}

HL_PRIM bool HL_NAME(ugc_query_set_return_children)(vuid handle, bool returnChildren) {
	if (!CheckInit() || handle == NULL) return false;
	return SteamUGC()->SetReturnChildren(hl_to_uint64(handle), returnChildren);
}

static void on_query_completed(vclosure *c, SteamUGCQueryCompleted_t *result, bool error) {
	if (!error && result->m_eResult == k_EResultOK) {
		HLValue v;
		v.Set("handle", result->m_handle);
		v.Set("resultsReturned", result->m_unNumResultsReturned);
		v.Set("totalResults", result->m_unTotalMatchingResults);
		v.Set("cached", result->m_bCachedData);
		dyn_call_result(c, v.value, error);
	} else {
		dyn_call_result(c, NULL, true);
	}
}
HL_PRIM CClosureCallResult<SteamUGCQueryCompleted_t>* HL_NAME(ugc_query_send_request)(vuid cHandle, vclosure *closure){
	if (!CheckInit() || cHandle == NULL) return NULL;
	ASYNC_CALL(SteamUGC()->SendQueryUGCRequest(hl_to_uint64(cHandle)), SteamUGCQueryCompleted_t, on_query_completed);
	return m_call;
}

HL_PRIM bool HL_NAME(ugc_query_release_request)(vuid cHandle) {
	if (!CheckInit() || cHandle == NULL) return false;
	return SteamUGC()->ReleaseQueryUGCRequest(hl_to_uint64(cHandle));
}

HL_PRIM varray *HL_NAME(ugc_query_get_key_value_tags)(vuid cHandle, int iIndex, int maxValueLength ){
	if (!CheckInit() || cHandle == NULL) return NULL;

	UGCQueryHandle_t handle = hl_to_uint64(cHandle);

	char key[255];
	char *value = new char[maxValueLength];
	int num = SteamUGC()->GetQueryUGCNumKeyValueTags(handle, iIndex);
	varray *ret = hl_alloc_array(&hlt_bytes, num<<1);
	vbyte **a = hl_aptr(ret, vbyte*);
	for (int i = 0; i < num; i++) {
		SteamUGC()->GetQueryUGCKeyValueTag(handle, iIndex, i, key, 255, value, maxValueLength);
		
		a[i*2] = hl_copy_bytes((vbyte*)key, (int)strlen(key) + 1);
		a[i*2+1] = hl_copy_bytes((vbyte*)value, (int)strlen(value) + 1);
	}
	delete value;
	return ret;
}

HL_PRIM varray *HL_NAME(ugc_query_get_children)(vuid cHandle, int iIndex, int maxChildren) {
	if (!CheckInit() || cHandle == NULL || maxChildren <= 0) return NULL;

	PublishedFileId_t *children = new PublishedFileId_t[maxChildren];
	if( !SteamUGC()->GetQueryUGCChildren(hl_to_uint64(cHandle), iIndex, children, maxChildren) ) {
		delete children;
		return NULL;
	}
	int num = 0;
	while( num < maxChildren ){
		if (!children[num]) break;
		num++;
	}
	varray *ret = hl_alloc_array(&hlt_bytes, num);
	vbyte **a = hl_aptr(ret, vbyte*);
	for (int i = 0; i < num; i++)
		a[i] = hl_of_uint64(children[i]);
	delete children;
	return ret;
}

HL_PRIM vbyte *HL_NAME(ugc_query_get_metadata)(vuid sHandle, int iIndex){
	if (!CheckInit() || sHandle == NULL) return NULL;
	char pchMetadata[5000];
	SteamUGC()->GetQueryUGCMetadata(hl_to_uint64(sHandle), iIndex, pchMetadata, 5000);
	return hl_copy_bytes((vbyte*)pchMetadata, (int)strlen(pchMetadata) + 1);
}

HL_PRIM vdynamic *HL_NAME(ugc_query_get_result)(vuid sHandle, int iIndex){
	if (!CheckInit() || sHandle == NULL) return NULL;

	UGCQueryHandle_t handle = hl_to_uint64(sHandle);
	SteamUGCDetails_t d;
	HLValue v;

	SteamUGC()->GetQueryUGCResult(handle, iIndex, &d);

	v.Set("id", d.m_nPublishedFileId);
	v.Set("result", (int)d.m_eResult);
	v.Set("fileType", (int)d.m_eFileType);
	v.Set("creatorApp", d.m_nCreatorAppID);
	v.Set("consumerApp", d.m_nConsumerAppID);
	v.Set("title", d.m_rgchTitle);
	v.Set("description", d.m_rgchDescription);
	v.Set("owner", d.m_ulSteamIDOwner);
	v.Set("timeCreated", d.m_rtimeCreated);
	v.Set("timeUpdated", d.m_rtimeUpdated);
	v.Set("timeAddedToUserList", d.m_rtimeAddedToUserList);
	v.Set("visibility", d.m_eVisibility);
	v.Set("banned", d.m_bBanned);
	v.Set("acceptedForUse", d.m_bAcceptedForUse);
	v.Set("tagsTruncated", d.m_bTagsTruncated);
	v.Set("tags", d.m_rgchTags);
	v.Set("file", d.m_hFile);
	v.Set("previewFile", d.m_hPreviewFile);
	v.Set("fileName", d.m_pchFileName);
	v.Set("fileSize", d.m_nFileSize);
	v.Set("previewFileSize", d.m_nPreviewFileSize);
	v.Set("url", d.m_rgchURL);
	v.Set("votesup", d.m_unVotesUp);
	v.Set("votesDown", d.m_unVotesDown);
	v.Set("score", d.m_flScore);
	v.Set("numChildren", d.m_unNumChildren);

	return v.value;
}

DEFINE_PRIM(_UID, ugc_query_create_all_request, _I32 _I32 _I32 _I32 _I32);
DEFINE_PRIM(_UID, ugc_query_create_user_request, _I32 _I32 _I32 _I32 _I32 _I32 _I32);
DEFINE_PRIM(_UID, ugc_query_create_details_request, _ARR);

DEFINE_PRIM(_BOOL, ugc_query_set_language, _UID _BYTES);
DEFINE_PRIM(_BOOL, ugc_query_set_search_text, _UID _BYTES);
DEFINE_PRIM(_BOOL, ugc_query_add_required_tag, _UID _BYTES);
DEFINE_PRIM(_BOOL, ugc_query_add_required_key_value_tag, _UID _BYTES _BYTES);
DEFINE_PRIM(_BOOL, ugc_query_add_excluded_tag, _UID _BYTES);
DEFINE_PRIM(_BOOL, ugc_query_set_return_metadata, _UID _BOOL);
DEFINE_PRIM(_BOOL, ugc_query_set_return_key_value_tags, _UID _BOOL);
DEFINE_PRIM(_BOOL, ugc_query_set_return_children, _UID _BOOL);

DEFINE_PRIM(_CRESULT, ugc_query_send_request, _UID _CALLB(_DYN));
DEFINE_PRIM(_BOOL, ugc_query_release_request, _UID);

DEFINE_PRIM(_ARR, ugc_query_get_key_value_tags, _UID _I32 _I32);
DEFINE_PRIM(_ARR, ugc_query_get_children, _UID _I32 _I32);
DEFINE_PRIM(_BYTES, ugc_query_get_metadata, _UID _I32);
DEFINE_PRIM(_DYN, ugc_query_get_result, _UID _I32);


//-----------------------------------------------------------------------------------------------------------
// UGC UPDATE
//-----------------------------------------------------------------------------------------------------------

static void on_item_created(vclosure *c, CreateItemResult_t *result, bool error) {
	if (!error && result->m_eResult == k_EResultOK) {
		HLValue v;
		v.Set("id", result->m_nPublishedFileId);
		v.Set("userNeedsLegalAgreement", result->m_bUserNeedsToAcceptWorkshopLegalAgreement);
		dyn_call_result(c, v.value, error);
	}
	else {
		dyn_call_result(c, NULL, true);
	}
}
HL_PRIM CClosureCallResult<CreateItemResult_t>* HL_NAME(ugc_item_create)(int appId, vclosure *closure) {
	if (!CheckInit()) return NULL;

	ASYNC_CALL(SteamUGC()->CreateItem(appId, k_EWorkshopFileTypeCommunity), CreateItemResult_t, on_item_created);
	return m_call;
}

HL_PRIM vuid HL_NAME(ugc_item_start_update)(int id, vuid itemID) {
	if (!CheckInit()) return NULL;

	UGCUpdateHandle_t handle = SteamUGC()->StartItemUpdate(id, hl_to_uint64(itemID));
	return hl_of_uint64(handle);
}

static void on_item_updated(vclosure *c, SubmitItemUpdateResult_t *result, bool error) {
	if (!error && result->m_eResult == k_EResultOK) {
		vdynamic d;
		d.t = &hlt_bool;
		d.v.b = result->m_bUserNeedsToAcceptWorkshopLegalAgreement;
		dyn_call_result(c, &d, error);
	}
	else {
		vdynamic d;
		d.t = &hlt_i32;
		d.v.i = result->m_eResult;
		dyn_call_result(c, &d, true);
	}
}

HL_PRIM CClosureCallResult<SubmitItemUpdateResult_t>* HL_NAME(ugc_item_submit_update)(vuid updateHandle, vbyte *changeNotes, vclosure *closure){
	if (!CheckInit() || updateHandle == NULL) return NULL;

	ASYNC_CALL(SteamUGC()->SubmitItemUpdate(hl_to_uint64(updateHandle), (char*)changeNotes), SubmitItemUpdateResult_t, on_item_updated);
 	return m_call;
}

HL_PRIM bool HL_NAME(ugc_item_set_update_language)(vuid updateHandle, vbyte *lang) {
	if (!CheckInit() || updateHandle == NULL) return false;
	return SteamUGC()->SetItemUpdateLanguage(hl_to_uint64(updateHandle), (char*)lang);
}

HL_PRIM bool HL_NAME(ugc_item_set_title)(vuid updateHandle, vbyte *title) {
	if (!CheckInit() || updateHandle == NULL) return false;
	return SteamUGC()->SetItemTitle(hl_to_uint64(updateHandle), (char*)title);
}

HL_PRIM bool HL_NAME(ugc_item_set_description)(vuid updateHandle, vbyte *description){
	if(!CheckInit() || updateHandle == NULL) return false;
	return SteamUGC()->SetItemDescription(hl_to_uint64(updateHandle), (char*)description);
}

HL_PRIM bool HL_NAME(ugc_item_set_metadata)(vuid updateHandle, vbyte *metadata) {
	if (!CheckInit() || updateHandle == NULL) return false;
	return SteamUGC()->SetItemMetadata(hl_to_uint64(updateHandle), (char*)metadata);
}

HL_PRIM bool HL_NAME(ugc_item_set_tags)(vuid updateHandle, varray *tags){
	if (!CheckInit() || updateHandle == NULL || tags == NULL) return false;

	SteamParamStringArray_t pTags;
	pTags.m_nNumStrings = tags->size;
	pTags.m_ppStrings = (const char**)hl_aptr(tags, vbyte*);

	return SteamUGC()->SetItemTags(hl_to_uint64(updateHandle), &pTags);
}

HL_PRIM bool HL_NAME(ugc_item_add_key_value_tag)(vuid updateHandle, vbyte *keyStr, vbyte *valueStr){
	if (!CheckInit() || updateHandle == NULL) return false;
	return SteamUGC()->AddItemKeyValueTag(hl_to_uint64(updateHandle), (char*)keyStr, (char*)valueStr);
}

HL_PRIM bool HL_NAME(ugc_item_remove_key_value_tags)(vuid updateHandle, vbyte *keyStr) {
	if (!CheckInit() || updateHandle == NULL) return false;
	return SteamUGC()->RemoveItemKeyValueTags(hl_to_uint64(updateHandle), (char*)keyStr);
}

HL_PRIM bool HL_NAME(ugc_item_set_visibility)(vuid updateHandle, int visibility) {
	if (!CheckInit() || updateHandle == NULL) return false;
	ERemoteStoragePublishedFileVisibility visibilityEnum = static_cast<ERemoteStoragePublishedFileVisibility>(visibility);
	return SteamUGC()->SetItemVisibility(hl_to_uint64(updateHandle), visibilityEnum);
}

HL_PRIM bool HL_NAME(ugc_item_set_content)(vuid updateHandle, vbyte *path){
	if (!CheckInit() || updateHandle == NULL) return false;
	return SteamUGC()->SetItemContent(hl_to_uint64(updateHandle), (char*)path);
}

HL_PRIM bool HL_NAME(ugc_item_set_preview_image)(vuid updateHandle, vbyte *path){
	if (!CheckInit() || updateHandle == NULL) return false;
	return SteamUGC()->SetItemPreview(hl_to_uint64(updateHandle), (char*)path);
}

DEFINE_PRIM(_CRESULT, ugc_item_create, _I32 _CALLB(_DYN));
DEFINE_PRIM(_UID, ugc_item_start_update, _I32 _UID);
DEFINE_PRIM(_CRESULT, ugc_item_submit_update, _UID _BYTES _CALLB(_BOOL));
DEFINE_PRIM(_BOOL, ugc_item_set_update_language, _UID _BYTES);
DEFINE_PRIM(_BOOL, ugc_item_set_title, _UID _BYTES);
DEFINE_PRIM(_BOOL, ugc_item_set_description, _UID _BYTES);
DEFINE_PRIM(_BOOL, ugc_item_set_metadata, _UID _BYTES);
DEFINE_PRIM(_BOOL, ugc_item_set_tags, _UID _ARR);
DEFINE_PRIM(_BOOL, ugc_item_add_key_value_tag, _UID _BYTES _BYTES);
DEFINE_PRIM(_BOOL, ugc_item_remove_key_value_tags, _UID _BYTES);
DEFINE_PRIM(_BOOL, ugc_item_set_visibility, _UID _I32);
DEFINE_PRIM(_BOOL, ugc_item_set_content, _UID _BYTES);
DEFINE_PRIM(_BOOL, ugc_item_set_preview_image, _UID _BYTES);
