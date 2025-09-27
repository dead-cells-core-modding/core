#include "steamwrap.h"

void CallbackHandler::OnUserStatsReceived( UserStatsReceived_t *pCallback ){
 	if (pCallback->m_nGameID != SteamUtils()->GetAppID()) return;
	SendEvent(UserStatsReceived, pCallback->m_eResult == k_EResultOK, NULL);
}

void CallbackHandler::OnUserStatsStored( UserStatsStored_t *pCallback ){
 	if (pCallback->m_nGameID != SteamUtils()->GetAppID()) return;
	SendEvent(UserStatsStored, pCallback->m_eResult == k_EResultOK, NULL);
}

void CallbackHandler::OnAchievementStored( UserAchievementStored_t *pCallback ){
 	if (pCallback->m_nGameID != SteamUtils()->GetAppID()) return;
	SendEvent(UserAchievementStored, true, pCallback->m_rgchAchievementName);
}

void CallbackHandler::FindLeaderboard(const char* name){
	m_leaderboards[name] = 0;
 	SteamAPICall_t hSteamAPICall = SteamUserStats()->FindLeaderboard(name);
 	m_callResultFindLeaderboard.Set(hSteamAPICall, this, &CallbackHandler::OnLeaderboardFound);
}

void CallbackHandler::OnLeaderboardFound(LeaderboardFindResult_t *pCallback, bool bIOFailure)
{
	// see if we encountered an error during the call
	if (pCallback->m_bLeaderboardFound && !bIOFailure)	{
		std::string leaderboardId = SteamUserStats()->GetLeaderboardName(pCallback->m_hSteamLeaderboard);
		m_leaderboards[leaderboardId] = pCallback->m_hSteamLeaderboard;
		SendEvent(LeaderboardFound, true, leaderboardId.c_str());
	}else{
		SendEvent(LeaderboardFound, false, NULL);
	}
}

bool CallbackHandler::UploadScore(const std::string& leaderboardId, int score, int detail){
	if (m_leaderboards.find(leaderboardId) == m_leaderboards.end() || m_leaderboards[leaderboardId] == 0)
		return false;

	SteamAPICall_t hSteamAPICall = SteamUserStats()->UploadLeaderboardScore(m_leaderboards[leaderboardId], k_ELeaderboardUploadScoreMethodKeepBest, score, &detail, 1);
	m_callResultUploadScore.Set(hSteamAPICall, this, &CallbackHandler::OnScoreUploaded);
 	return true;
}


static std::string toLeaderboardScore(const char* leaderboardName, int score, int detail, int rank){
	std::ostringstream data;
	data << leaderboardName << "," << score << "," << detail << "," << rank;
	return data.str();
}

void CallbackHandler::OnScoreUploaded(LeaderboardScoreUploaded_t *pCallback, bool bIOFailure){
	if (pCallback->m_bSuccess && !bIOFailure){
		std::string leaderboardName = SteamUserStats()->GetLeaderboardName(pCallback->m_hSteamLeaderboard);
		std::string data = toLeaderboardScore(SteamUserStats()->GetLeaderboardName(pCallback->m_hSteamLeaderboard), pCallback->m_nScore, -1, pCallback->m_nGlobalRankNew);
		SendEvent(ScoreUploaded, true, data.c_str());
	}else if (pCallback != NULL && pCallback->m_hSteamLeaderboard != 0) {
		SendEvent(ScoreUploaded, false, SteamUserStats()->GetLeaderboardName(pCallback->m_hSteamLeaderboard));
	}else{
		SendEvent(ScoreUploaded, false, NULL);
	}
}

bool CallbackHandler::DownloadScores(const std::string& leaderboardId, int numBefore, int numAfter){
	if (m_leaderboards.find(leaderboardId) == m_leaderboards.end() || m_leaderboards[leaderboardId] == 0)
		return false;

 	// load the specified leaderboard data around the current user
 	SteamAPICall_t hSteamAPICall = SteamUserStats()->DownloadLeaderboardEntries(m_leaderboards[leaderboardId], k_ELeaderboardDataRequestGlobalAroundUser, -numBefore, numAfter);
	m_callResultDownloadScore.Set(hSteamAPICall, this, &CallbackHandler::OnScoreDownloaded);

 	return true;
}

void CallbackHandler::OnScoreDownloaded(LeaderboardScoresDownloaded_t *pCallback, bool bIOFailure){
	if (bIOFailure)	{
		SendEvent(ScoreDownloaded, false, NULL);
		return;
	}

	std::string leaderboardId = SteamUserStats()->GetLeaderboardName(pCallback->m_hSteamLeaderboard);

	int numEntries = pCallback->m_cEntryCount;
	if (numEntries > 10) numEntries = 10;

	std::ostringstream data;
	bool haveData = false;

	for (int i=0; i<numEntries; i++){
		int score = 0;
		int details[1];
		LeaderboardEntry_t entry;
		SteamUserStats()->GetDownloadedLeaderboardEntry(pCallback->m_hSteamLeaderboardEntries, i, &entry, details, 1);
		if (entry.m_cDetails != 1) continue;

		if (haveData)
			data << ";";
		data << toLeaderboardScore(leaderboardId.c_str(), entry.m_nScore, details[0], entry.m_nGlobalRank).c_str();
		haveData = true;
	}

	if (haveData)
		SendEvent(ScoreDownloaded, true, data.str().c_str());
	else
		SendEvent(ScoreDownloaded, true, toLeaderboardScore(leaderboardId.c_str(), -1, -1, -1).c_str());
}

void CallbackHandler::RequestGlobalStats(){
 	SteamAPICall_t hSteamAPICall = SteamUserStats()->RequestGlobalStats(0);
 	m_callResultRequestGlobalStats.Set(hSteamAPICall, this, &CallbackHandler::OnGlobalStatsReceived);
}

void CallbackHandler::OnGlobalStatsReceived(GlobalStatsReceived_t* pResult, bool bIOFailure){
	if (!bIOFailure){
		if (pResult->m_nGameID != SteamUtils()->GetAppID()) return;
		SendEvent(GlobalStatsReceived, pResult->m_eResult == k_EResultOK, NULL);
	}else{
		SendEvent(GlobalStatsReceived, false, NULL);
	}
}

//-----------------------------------------------------------------------------------------------------------

HL_PRIM bool HL_NAME(request_stats)(){
	if (!CheckInit()) return false;
	return SteamUserStats()->RequestCurrentStats();
}
DEFINE_PRIM(_BOOL, request_stats, _NO_ARG);

HL_PRIM int HL_NAME(get_stat_int)(vbyte *name){
	if (!CheckInit()) return 0;

	int val = 0;
	SteamUserStats()->GetStat((char*)name, &val);
	return val;
}
DEFINE_PRIM(_I32, get_stat_int, _BYTES);

HL_PRIM double HL_NAME(get_stat_float)(vbyte *name){
	if (!CheckInit()) return 0.0;

	float val = 0.0;
	SteamUserStats()->GetStat((char*)name, &val);
	return (double)val;
}
DEFINE_PRIM(_F64, get_stat_float, _BYTES);

HL_PRIM bool HL_NAME(set_stat_int)(vbyte *name, int val){
	if (!CheckInit()) return false;
	return SteamUserStats()->SetStat((char*)name, val);
}
DEFINE_PRIM(_BOOL, set_stat_int, _BYTES _I32);

HL_PRIM bool HL_NAME(set_stat_float)(vbyte *name, double val){
	if (!CheckInit()) return false;
	return SteamUserStats()->SetStat((char*)name, (float)val);
}
DEFINE_PRIM(_BOOL, set_stat_float, _BYTES _F64);

HL_PRIM bool HL_NAME(store_stats)(){
	if (!CheckInit()) return false;
	return SteamUserStats()->StoreStats();
}
DEFINE_PRIM(_BOOL, store_stats, _NO_ARG);

//-----------------------------------------------------------------------------------------------------------

HL_PRIM bool HL_NAME(set_achievement)(vbyte *name){
	if (!CheckInit()) return false;

	SteamUserStats()->SetAchievement((char*)name);
	return SteamUserStats()->StoreStats();
}
DEFINE_PRIM(_BOOL, set_achievement, _BYTES);

HL_PRIM bool HL_NAME(get_achievement)(vbyte *name) {
	if (!CheckInit()) return false;
	bool achieved = false;
	SteamUserStats()->GetAchievement((char*)name, &achieved);
	return achieved;
}
DEFINE_PRIM(_BOOL, get_achievement, _BYTES);

HL_PRIM vbyte *HL_NAME(get_achievement_display_attribute)(vbyte *name, vbyte *key){
	if (!CheckInit()) return (vbyte*)"";
	return (vbyte*)SteamUserStats()->GetAchievementDisplayAttribute((char*)name, (char*)key);
}
DEFINE_PRIM(_BYTES, get_achievement_display_attribute, _BYTES _BYTES);

HL_PRIM int HL_NAME(get_num_achievements)(){
	if (!CheckInit()) return 0;
	return (int)SteamUserStats()->GetNumAchievements();
}
DEFINE_PRIM(_I32, get_num_achievements, _NO_ARG);

HL_PRIM vbyte *HL_NAME(get_achievement_name)(int index){
	if (!CheckInit()) return (vbyte*)"";
	return (vbyte*)SteamUserStats()->GetAchievementName(index);
}
DEFINE_PRIM(_BYTES, get_achievement_name, _I32);

HL_PRIM bool HL_NAME(clear_achievement)(vbyte *name){
	if (!CheckInit()) return false;
	SteamUserStats()->ClearAchievement((char*)name);
	return SteamUserStats()->StoreStats();
}
DEFINE_PRIM(_BOOL, clear_achievement, _BYTES);

HL_PRIM bool HL_NAME(indicate_achievement_progress)(vbyte *name, int numCurProgres, int numMaxProgress){
	if (!CheckInit()) return false;
	return SteamUserStats()->IndicateAchievementProgress((char*)name, numCurProgres, numMaxProgress);
}
DEFINE_PRIM(_BOOL, indicate_achievement_progress, _BYTES _I32 _I32);

//-----------------------------------------------------------------------------------------------------------

HL_PRIM bool HL_NAME(find_leaderboard)(vbyte *name) {
	if (!CheckInit()) return false;
	s_callbackHandler->FindLeaderboard((char*)name);
 	return true;
}
DEFINE_PRIM(_BOOL, find_leaderboard, _BYTES);

HL_PRIM bool HL_NAME(upload_score)(vbyte *name, int score, int detail){
	if (!CheckInit()) return false;
	return s_callbackHandler->UploadScore((char*)name, score, detail);
}
DEFINE_PRIM(_BOOL, upload_score, _BYTES _I32 _I32);

HL_PRIM bool HL_NAME(download_scores)(vbyte *name, int numBefore, int numAfter) {
	if (!CheckInit()) return false;
	return s_callbackHandler->DownloadScores((char*)name, numBefore, numAfter);
}
DEFINE_PRIM(_BOOL, download_scores, _BYTES _I32 _I32);

HL_PRIM bool HL_NAME(request_global_stats)() {
	if (!CheckInit()) return false;
	s_callbackHandler->RequestGlobalStats();
	return true;
}
DEFINE_PRIM(_BOOL, request_global_stats, _NO_ARG);

HL_PRIM int HL_NAME(get_global_stat)(vbyte *name){
	if (!CheckInit()) return 0;

	int64 val;
	SteamUserStats()->GetGlobalStat((char*)name, &val);
	return (int)val;
}
DEFINE_PRIM(_I32, get_global_stat, _BYTES);
