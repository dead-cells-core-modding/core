package steam;

enum abstract ServerMode(Int) {
	var NoAuthentification = 1;
	var Authentification = 2;
	var AuthentificationAndSecure = 3;
}

@:hlNative("steam")
class GameServer {

	static var initDone = false;
	static var globalEvents = new Map<Int,Dynamic->Void>();

	@:noComplete public static function registerGlobalEvent( event : Int, callb : Dynamic -> Void ) {
		globalEvents.set(event, callb);
	}

	public static function init( ip : sys.net.Host, port : Int, gameport : Int, queryPort : Int, serverMode : ServerMode, version : String ) {
		var ip = ip.ip;
		var convIP = (ip >>> 24) | ((ip >> 8) & 0xFF00) | ((ip << 8) & 0xFF0000) | (ip << 24);
		if( !gameserver_init(convIP, port, gameport, queryPort, serverMode, @:privateAccess version.toUtf8()) )
			return false;
		if( initDone ) return true;
		haxe.MainLoop.add(runGameServer);
		gameserver_setup(onGlobalEvent);
		return true;
	}

	public static function setConfig( modDir : String, product : String, desc : String ) {
		@:privateAccess gameserver_config( modDir.toUtf8(), product.toUtf8(), desc.toUtf8() );
	}

	public static function setInfo( maxPlayers, password, serverName : String, botCount : Int, mapName : String ) {
		@:privateAccess gameserver_info( maxPlayers, password, serverName.toUtf8(), botCount, mapName.toUtf8() );
	}

	static function onGlobalEvent( event : Int, data : Dynamic ) {
		var callb = globalEvents.get(event);
		if( callb == null )
			callb = @:privateAccess Api.globalEvents.get(event);
		if( callb != null )
			callb(data);
		else
			customTrace("Unhandled server event #"+event+" ("+data+")");
	}

	public static dynamic function customTrace(str:String) {
		Sys.println(str);
	}

	@:hlNative("steam", "gameserver_enable_heartbeats")
	public static function enableHeartbeats( b : Bool ) {
	}

	@:hlNative("steam", "gameserver_get_public_ip")
	public static function getPublicIP() : Int {
		return 0;
	}

	@:hlNative("steam", "gameserver_get_steam_id")
	public static function getSteamID() : UID {
		return null;
	}

	public static function logonAnonymous( onLogin : Bool -> Void, retries = 3 ) {

		var ucb = 100;
		//SteamServersConnected_t
		registerGlobalEvent(ucb + 1, function(_:Dynamic) {
			if( onLogin == null ) return;
			registerGlobalEvent(ucb + 3, onDisconnected);
			onLogin(true);
			onLogin = null;
		});
		function failure(r:Dynamic) {
			customTrace("CONNECT FAILURE " + r);
			if(retries > 0) {
				logonAnonymous(onLogin, retries-1);
			}
			else {
				if( onLogin == null )
					return;
				onLogin(false);
			}
			onLogin = null;
		}
		//SteamServerConnectFailure_t
		registerGlobalEvent(ucb + 2, failure);
		//SteamServersDisconnected_t
		registerGlobalEvent(ucb + 3, failure);
		registerGlobalEvent(ucb + 15, function(_:Dynamic) { /* ignore VAC flag */ });

		gameserver_logon_anonymous();
	}

	static function onDisconnected(_:Dynamic) {
		customTrace("Gameserver disconnected, retrying...");
		logonAnonymous(function(b) {
			if( b ) {
				// seems like it's never called (no SteamServersConnected_t after retry?)
				customTrace("Gameserver reconnected as "+getSteamID().getBytes().toHex());
				return;
			}
			haxe.Timer.delay(onDisconnected.bind(null), 10000);
		});
	}

	@:hlNative("steam", "gameserver_shutdown")
	public static function shutdown() {
	}

	static function runGameServer() {
		gameserver_runcallbacks();
	}

	static function gameserver_init( ip : Int, port : Int, gameport : Int, queryport : Int, serverMode : ServerMode, version : hl.Bytes ) : Bool {
		return false;
	}

	static function gameserver_runcallbacks() {
	}

	static function gameserver_logon_anonymous() {
	}

	static function gameserver_setup( onGlobalEvent : Int -> Dynamic -> Void ) {
	}

	static function gameserver_config( modDir : hl.Bytes, name : hl.Bytes, desc : hl.Bytes ) {
	}

	static function gameserver_info( maxPlayers : Int, password : Bool, serverName : hl.Bytes, botCount : Int, mapName : hl.Bytes ) {
	}

	public static function requestInternetServerList( appId : Int, ?filters : {}, ?onResults : Array<{ id : UID, ip : Int, port : Int, ping : Int }> -> Void ) {
		var fields = filters == null ? [] : Reflect.fields(filters);
		var n = new hl.NativeArray(fields.length * 2);
		var p = 0;
		for( f in fields ) {
			n[p++] = @:privateAccess f.toUtf8();
			n[p++] = @:privateAccess Std.string(Reflect.field(filters,f)).toUtf8();
		}
		var results = [];
		request_internet_server_list(appId, n, function(r) if( r != null ) results.push(r) else onResults(results));
	}

	static function request_internet_server_list( appId : Int, filters : hl.NativeArray<hl.Bytes>, onResult : Dynamic -> Void ) {
	}

}