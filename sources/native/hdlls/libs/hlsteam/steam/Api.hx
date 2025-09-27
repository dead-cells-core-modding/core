package steam;

import haxe.Int64;
import steam.helpers.Util;

private enum LeaderboardOp
{
	FIND(id:String);
	UPLOAD(score:LeaderboardScore);
	DOWNLOAD(id:String);
}

enum abstract SteamNotificationPosition(Int) to Int
{
	var TopLeft = 0;
	var TopRight = 1;
	var BottomLeft = 2;
	var BottomRight = 3;
}

typedef ControllerHandle = Int64;
typedef ControllerActionSetHandle = Int;
typedef ControllerDigitalActionHandle = Int;
typedef ControllerAnalogActionHandle = Int;

enum abstract EventType(Int) {
	var None                              = 0;
	var GamepadTextInputDismissed         = 1;
	var UserStatsReceived                 = 2;
	var UserStatsStored                   = 3;
	var UserAchievementStored             = 4;
	var LeaderboardFound                  = 5;
	var ScoreUploaded                     = 6;
	var ScoreDownloaded                   = 7;
	var GlobalStatsReceived               = 8;
}

@:hlNative("steam")
class Api
{
	/*************PUBLIC***************/

	/**
	 * Whether the Steam API is detected & initialized or not. If false, all calls will fail
	 */
	public static var active(default, null):Bool = false;
	public static var appId(default, null) : Int;

	/**
	 * The Steam Controller API
	 */
	public static var controllers(default, null):Controller;

	// Deprecated User-settable callbacks:

	public static var whenGamepadTextInputDismissed:String->Void;
	public static var whenAchievementStored:String->Void;
	public static var whenLeaderboardScoreDownloaded:LeaderboardScore->Void;
	public static var whenLeaderboardScoreUploaded:LeaderboardScore->Void;

	// User-settable Callbacks

	public static var onOverlay : Bool -> Void;

	/**
	 * @param appId_	Your Steam APP ID (the numbers on the end of your store page URL - store.steampowered.com/app/XYZ)
	 */
	public static function init(appId_:Int) {
		if (active) return true;

		appId = appId_;
		leaderboardIds = new Array<String>();
		leaderboardOps = new List<LeaderboardOp>();

		// PersonaStateChange_t
		registerGlobalEvent(300 + 4, function(data:{user:UID, flags:haxe.EnumFlags<User.Changed>}) {
			@:privateAccess User.fromUID(data.user).onDataUpdated(data.flags);
		});

		// GameOverlayActivated_t
		registerGlobalEvent(300 + 31, function(data:{active: Bool}){
			if( onOverlay != null )
				onOverlay( data.active );
		});

		// GetAuthSessionTicketResponse_t
		registerGlobalEvent(100 + 63, function(data:{authTicket: Int, result:Int}){
			var cb = authTicketCallbacks.get(data.authTicket);
			if( cb !=null ){
				cb(data.result == 1);
				authTicketCallbacks.remove(data.authTicket);
			}
		});

		// if we get this far, the dlls loaded ok and we need Steam to init.
		// otherwise, we're trying to run the Steam version without the Steam client
		active = _Init(steamWrap_onEvent, onGlobalEvent);

		if (active) {
			//customTrace("Steam active");
			_RequestStats();
			_RequestGlobalStats();

			//initialize other API's:
			controllers = new Controller(customTrace);

			haxe.MainLoop.add(sync);
		}
		else {
			customTrace("Steam failed to activate");
		}
		return active;
	}

	static var globalEvents = new Map<Int,Dynamic->Void>();
	static var authTicketCallbacks : Map<Int, Bool->Void> = new Map();

	@:noComplete public static function registerGlobalEvent( event : Int, callb : Dynamic -> Void ) {
		globalEvents.set(event, callb);
	}

	static function onGlobalEvent( event : Int, data : Dynamic ) {
		var callb = globalEvents.get(event);
		if( callb != null )
			callb(data);
		else
			customTrace("Unhandled global event #"+event+" ("+data+")");
	}

	public static function setNotificationPosition( pos:SteamNotificationPosition ) {
	}

	public static function sync() {
		if (!active) return;
		_RunCallbacks();

		if (wantStoreStats) {
			wantStoreStats = false;
			_StoreStats();
		}
	}

	/*************PUBLIC***************/

	/**
	 * Clear an achievement
	 * @param	id	achievement identifier
	 * @return
	 */
	public static function clearAchievement(id:String):Bool {
		return active && report("clearAchievement", [id], _ClearAchievement(@:privateAccess id.toUtf8()));
	}

	public static function downloadLeaderboardScore(id:String):Bool {
		if (!active) return false;
		var startProcessingNow = (leaderboardOps.length == 0);
		findLeaderboardIfNecessary(id);
		leaderboardOps.add(LeaderboardOp.DOWNLOAD(id));
		if (startProcessingNow) processNextLeaderboardOp();
		return true;
	}

	private static function findLeaderboardIfNecessary(id:String) {
		if (!Lambda.has(leaderboardIds, id) && !Lambda.exists(leaderboardOps, function(op) { return Type.enumEq(op, FIND(id)); }))
		{
			leaderboardOps.add(LeaderboardOp.FIND(id));
		}
	}

	/**
	 * Returns achievement status.
	 * @param id Achievement API name.
	 * @return true, if achievement already achieved, false otherwise.
	 */
	public static function getAchievement(id:String):Bool {
		return active && _GetAchievement(@:privateAccess id.toUtf8());
	}

	/**
	 * Returns human-readable achievement description.
	 * @param id Achievement API name.
	 * @return UTF-8 string with achievement description.
	 */
	public static function getAchievementDescription(id:String):String {
		if (!active) return null;
		return @:privateAccess String.fromUTF8(_GetAchievementDisplayAttribute(@:privateAccess id.toUtf8(), @:privateAccess "desc".toUtf8()));
	}

	/**
	 * Returns human-readable achievement name.
	 * @param id Achievement API name.
	 * @return UTF-8 string with achievement name.
	 */
	public static function getAchievementName(id:String):String {
		if (!active) return null;
		return @:privateAccess String.fromUTF8(_GetAchievementDisplayAttribute(@:privateAccess id.toUtf8(), @:privateAccess "name".toUtf8()));
	}

	public static function getCurrentGameLanguage():String {
		var l = _GetCurrentGameLanguage();
		return l==null ? null : @:privateAccess String.fromUTF8(l);
	}

	public static function getCurrentBetaName():String {
		var l = _GetCurrentBetaName();
		return l == null ? null : @:privateAccess String.fromUTF8(l);
	}

	/**
	 * Get a stat from steam as a float
	 * Kinda awkwardly returns 0 on errors and uses 0 for checking success
	 * @param	id
	 * @return
	 */
	public static function getStatFloat(id:String):Float {
		if (!active)
			return 0;
		var val = _GetStatFloat(@:privateAccess id.toUtf8());
		report("getStat", [id], val != 0);
		return val;
	}

	/**
	 * Get a stat from steam as an integer
	 * Kinda awkwardly returns 0 on errors and uses 0 for checking success
	 * @param	id
	 * @return
	 */
	public static function getStatInt(id:String):Int {
		if (!active)
			return 0;
		var val = _GetStatInt(@:privateAccess id.toUtf8());
		report("getStat", [id], val != 0);
		return val;
	}

	public static function getUser() {
		return active ? User.fromUID(get_steam_id()) : null;
	}

	static function get_steam_id() : UID {
		return null;
	}

	public static function indicateAchievementProgress(id:String, curProgress:Int, maxProgress:Int):Bool {
		return active && report("indicateAchivevementProgress", [id, Std.string(curProgress), Std.string(maxProgress)], _IndicateAchievementProgress(@:privateAccess id.toUtf8(), curProgress, maxProgress));
	}

	public static function isOverlayEnabled():Bool {
		if (!active)
			return false;
		return _IsOverlayEnabled();
	}

	public static function isDlcInstalled( appid : Int ) : Bool {
		if( !active )
			return false;
		return _IsDlcInstalled(appid);
	}

	@:hlNative("steam","is_dlc_installed")
	static function _IsDlcInstalled( appid : Int ) : Bool {
		return false;
	}

	@:hlNative("?steam","get_app_install_dir")
	static function _GetAppInstallDir( appid : Int ) : hl.Bytes {
		return null;
	}

	public static function getAppInstallDir( appid : Int ) : String {
		var dir = _GetAppInstallDir(appid);
		if(dir == null)
			return null;
		return @:privateAccess String.fromUTF8(dir);
	}

	@:hlNative("?steam","is_app_owned")
	public static function isAppOwned( appid : Int ) : Bool {
		return false;
	}

	@:hlNative("?steam","is_app_installed")
	public static function isAppInstalled( appid : Int ) : Bool {
		return false;
	}

	public static function BOverlayNeedsPresent() {
		if (!active)
			return false;
		return _BOverlayNeedsPresent();
	}

	public static function isSteamInBigPictureMode() {
		if (!active)
			return false;
		return _IsSteamInBigPictureMode();
	}

	public static function isSteamRunningOnSteamDeck() {
		if (!active)
			return false;
		return _IsSteamRunningOnSteamDeck();
	}

	public static function isSteamRunning() {
		if (!active)
			return false;
		return _IsSteamRunning();
	}

	public static function openOverlay(url:String) {
		if (!active) return;
		_OpenOverlay(@:privateAccess url.toUtf8());
	}

	public static function restartIfNecessary( appId : Int ):Bool {
		return _RestartAppIfNecessary( appId );
	}

	public static function shutdown() {
		if (!active) return;
		_Shutdown();
	}

	public static function setAchievement(id:String):Bool {
		return active && report("setAchievement", [id], _SetAchievement(@:privateAccess id.toUtf8()));
	}

	/**
	 * Returns achievement "hidden" flag.
	 * @param id Achievement API name.
	 * @return true, if achievement is flagged as hidden, false otherwise.
	 */
	public static function isAchievementHidden(id:String):Bool {
		return active && @:privateAccess String.fromUTF8(_GetAchievementDisplayAttribute(@:privateAccess id.toUtf8(), @:privateAccess "hidden".toUtf8())) == "1";
	}

	/**
	 * Returns amount of achievements.
	 * Used for iterating achievements. In general games should not need these functions because they should have a
	 * list of existing achievements compiled into them.
	 */
	public static function getNumAchievements():Int {
		if (!active) return 0;
		return _GetNumAchievements();
	}

	/**
	 * Returns achievement API name from its index in achievement list.
	 * @param index Achievement index in range [0,getNumAchievements].
	 * @return Achievement API name.
	 */
	public static function getAchievementAPIName(index:Int):String {
		if (!active) return null;
		return @:privateAccess String.fromUTF8(_GetAchievementName(index));
	}

	/**
	 * Sets a steam stat as a float
	 * @param	id Stat API name
	 * @param	val
	 * @return
	 */
	public static function setStatFloat(id:String, val:Float):Bool {
		return active && report("setStatFloat", [id, Std.string(val)], _SetStatFloat(@:privateAccess id.toUtf8(), val));
	}

	/**
	 * Sets a steam stat as an int
	 * @param	id Stat API name
	 * @param	val
	 * @return
	 */
	public static function setStatInt(id:String, val:Int):Bool {
		return active && report("setStatInt", [id, Std.string(val)], _SetStatInt(@:privateAccess id.toUtf8(), val));
	}

	public static function storeStats():Bool {
		return active && report("storeStats", [], _StoreStats());
	}

	public static function uploadLeaderboardScore(score:LeaderboardScore):Bool {
		if (!active) return false;
		var startProcessingNow = (leaderboardOps.length == 0);
		findLeaderboardIfNecessary(score.leaderboardId);
		leaderboardOps.add(LeaderboardOp.UPLOAD(score));
		if (startProcessingNow) processNextLeaderboardOp();
		return true;
	}

	public static function getAuthTicket( ?onReady : Bool->Void ) : haxe.io.Bytes {
		var size = 0;
		var authTicket = 0;
		var ticket = _GetAuthTicket(size,authTicket);
		if( size == 0 || authTicket == 0 )
			return null;
		if( onReady != null ){
			authTicketCallbacks.set(authTicket, onReady);
			// Timeout
			haxe.Timer.delay(function(){
				var cb = authTicketCallbacks.get(authTicket);
				if( cb != null ){
					cb(false);
					authTicketCallbacks.remove(authTicket);
				}
			}, 15000); //15sec
		}

		return ticket.toBytes(size);
	}

	public static function getEncryptedAppTicket(data : haxe.io.Bytes, onReady : haxe.io.Bytes->Void ) : Void {
		var hlb = data != null ? hl.Bytes.fromBytes(data) : null;
		var len = data != null ? data.length : 0;
		_RequestEncryptedAppTicket(hlb, len, function(rd : hl.Bytes, rds : Int) {
			if(onReady == null)
				return;
			var d = rd != null ? rd.toBytes(rds) : null;
			onReady(d);
		});
	}

	//PRIVATE:

	private static var haveGlobalStats:Bool;
	private static var haveReceivedUserStats:Bool;
	private static var wantStoreStats:Bool;

	private static var leaderboardIds:Array<String>;
	private static var leaderboardOps:List<LeaderboardOp>;

	public static dynamic function customTrace(str:String) {
		Sys.println(str);
	}

	private static function processNextLeaderboardOp() {
		var op = leaderboardOps.pop();
		if (op == null) return;

		switch (op) {
			case FIND(id):
				if (!report("Leaderboard.FIND", [id], _FindLeaderboard(@:privateAccess id.toUtf8())))
					processNextLeaderboardOp();
			case UPLOAD(score):
				if (!report("Leaderboard.UPLOAD", [score.toString()], _UploadScore(@:privateAccess score.leaderboardId.toUtf8(), score.score, score.detail)))
					processNextLeaderboardOp();
			case DOWNLOAD(id):
				if (!report("Leaderboard.DOWNLOAD", [id], _DownloadScores(@:privateAccess id.toUtf8(), 0, 0)))
					processNextLeaderboardOp();
		}
	}

	private static function report(func:String, params:Array<String>, result:Bool):Bool {
		var str = "[STEAM] " + func + "(" + params.join(",") + ") " + (result ? " SUCCEEDED" : " FAILED");
		customTrace(str);
		return result;
	}

	private static function steamWrap_onEvent( type : EventType, success : Bool, data : hl.Bytes ) : Void {
		var data:String = data == null ? null : @:privateAccess String.fromUTF8(data);

		customTrace("[STEAM] Event@" + type + (success ? " SUCCESS" : " FAIL") + (data == null ? "" : " (" + data + ")"));

		switch (type) {
			case UserStatsReceived:
				haveReceivedUserStats = success;

			case UserStatsStored:
				// retry next frame if failed
				wantStoreStats = !success;

			case UserAchievementStored:
				if (whenAchievementStored != null) whenAchievementStored(data);

			case GamepadTextInputDismissed:
				if (whenGamepadTextInputDismissed != null) {
					if (success) {
						whenGamepadTextInputDismissed(controllers.getEnteredGamepadTextInput());
					}
					else {
						whenGamepadTextInputDismissed(null);
					}
				}

			case GlobalStatsReceived:
				haveGlobalStats = success;

			case LeaderboardFound:
				if (success) {
					leaderboardIds.push(data);
				}
				processNextLeaderboardOp();
			case ScoreDownloaded:
				if (success) {
					var scores = data.split(";");
					for (score in scores) {
						var score = LeaderboardScore.fromString(data);
						if (score != null && whenLeaderboardScoreDownloaded != null) whenLeaderboardScoreDownloaded(score);
					}
				}
				processNextLeaderboardOp();
			case ScoreUploaded:
				if (success) {
					var score = LeaderboardScore.fromString(data);
					if (score != null && whenLeaderboardScoreUploaded != null) whenLeaderboardScoreUploaded(score);
				}
				processNextLeaderboardOp();
			case None:
		}
	}

	@:hlNative("steam","init") private static function _Init( onEvent : EventType -> Bool -> hl.Bytes -> Void, onGlobalEvent : Int -> Dynamic -> Void ) : Bool { return false; }
	@:hlNative("steam","shutdown") private static function _Shutdown(): Void{};
	@:hlNative("steam","run_callbacks") private static function _RunCallbacks(): Void{};
	@:hlNative("steam","request_stats") private static function _RequestStats() : Bool { return false; }
	@:hlNative("steam","get_stat_float") private static function _GetStatFloat( name : hl.Bytes ) : Float { return 0.; }
	@:hlNative("steam","get_stat_int") private static function _GetStatInt( name : hl.Bytes ) : Int { return 0; }
	@:hlNative("steam","set_stat_float") private static function _SetStatFloat( name : hl.Bytes, val : Float ) : Bool { return false; }
	@:hlNative("steam","set_stat_int") private static function _SetStatInt( name : hl.Bytes, val : Int ) : Bool { return false; }
	@:hlNative("steam","set_achievement") private static function _SetAchievement( name : hl.Bytes ) : Bool { return false; }
	@:hlNative("steam","get_achievement") private static function _GetAchievement( name : hl.Bytes ) : Bool { return false; }
	@:hlNative("steam","get_achievement_display_attribute") private static function _GetAchievementDisplayAttribute( name : hl.Bytes, key : hl.Bytes ) : hl.Bytes { return null; }
	@:hlNative("steam","get_num_achievements") private static function _GetNumAchievements() : Int { return 0; }
	@:hlNative("steam","get_achievement_name") private static function _GetAchievementName( index : Int ) : hl.Bytes { return null; }
	@:hlNative("steam","clear_achievement") private static function _ClearAchievement( name : hl.Bytes ) : Bool { return false; }
	@:hlNative("steam","indicate_achievement_progress") private static function _IndicateAchievementProgress( name : hl.Bytes, curProgress : Int, maxProgress : Int ) : Bool { return false; }
	@:hlNative("steam","store_stats") private static function _StoreStats() : Bool { return false; }
	@:hlNative("steam","find_leaderboard") private static function _FindLeaderboard( name : hl.Bytes ) : Bool { return false; }
	@:hlNative("steam","upload_score") private static function _UploadScore( name : hl.Bytes, score : Int, detail : Int ) : Bool { return false; }
	@:hlNative("steam","download_scores") private static function _DownloadScores( name : hl.Bytes, before: Int, afeter : Int ) : Bool { return false; }
	@:hlNative("steam","request_global_stats") private static function _RequestGlobalStats() : Bool { return false; }
	@:hlNative("steam","get_global_stat") private static function _GetGlobalStat( name : hl.Bytes ) : Int { return 0; }
	@:hlNative("steam","restart_app_if_necessary") private static function _RestartAppIfNecessary( appId : Int ) : Bool { return false; }
	@:hlNative("steam","is_overlay_enabled") private static function _IsOverlayEnabled() : Bool { return false; }
	@:hlNative("steam","boverlay_needs_present") private static function _BOverlayNeedsPresent() : Bool { return false; }
	@:hlNative("steam","is_steam_in_big_picture_mode") private static function _IsSteamInBigPictureMode() : Bool { return false; }
	@:hlNative("?steam","is_steam_running_on_steam_deck") private static function _IsSteamRunningOnSteamDeck() : Bool { return false; }
	@:hlNative("steam","is_steam_running") private static function _IsSteamRunning() : Bool { return false; }
	@:hlNative("steam","get_current_game_language") private static function _GetCurrentGameLanguage() : hl.Bytes { return null; }
	@:hlNative("steam","get_auth_ticket") private static function _GetAuthTicket( size : hl.Ref<Int>, authTicket : hl.Ref<Int> ) : hl.Bytes { return null; }
	@:hlNative("?steam","request_encrypted_app_ticket") private static function _RequestEncryptedAppTicket( data : hl.Bytes, size : Int, encryptedAppTicket : (hl.Bytes, Int) -> Void ) : Void { return; }
	@:hlNative("steam","open_overlay") private static function _OpenOverlay( url : hl.Bytes ) : Bool { return false; }
	@:hlNative("steam","get_current_beta_name") private static function _GetCurrentBetaName() : hl.Bytes { return null; }
	@:hlNative("?steam","is_subscribed") public static function isSubscribed() : Bool { return false; }
	@:hlNative("?steam","is_subscribed_from_family_sharing") public static function isSubscribedFromFamilySharing() : Bool { return false; }
	@:hlNative("?steam","is_subscribed_from_free_weekend") public static function isSubscribedFromFreeWeekend() : Bool { return false; }
}

class LeaderboardScore {
	public var leaderboardId:String;
	public var score:Int;
	public var detail:Int;
	public var rank:Int;

	public function new(leaderboardId_:String, score_:Int, detail_:Int, rank_:Int=-1) {
		leaderboardId = leaderboardId_;
		score = score_;
		detail = detail_;
		rank = rank_;
	}

	public function toString():String {
		return leaderboardId + "," + score + "," + detail + "," + rank;
	}

	public static function fromString(str:String):LeaderboardScore {
		var tokens = str.split(",");
		if (tokens.length == 4)
			return new LeaderboardScore(tokens[0], Util.str2Int(tokens[1]), Util.str2Int(tokens[2]), Util.str2Int(tokens[3]));
		else
			return null;
	}
}

