import Sys.println in log;

class GameServer {

	public static function main() {
		if( Sys.args()[0] == "-client" ) {
			GameClient.main();
			return;
		}

		var config : Dynamic = haxe.Json.parse(sys.io.File.getContent("config.json"));
		var host = new sys.net.Host(config.host);

		log("Starting server on " + host.toString() + ":" + config.port);

		if( !sys.FileSystem.exists("steam_appid.txt") )
			throw "Missing steam_appid.txt";

		if( !steam.GameServer.init(host, config.port, config.gamePort, config.queryPort, Authentification, config.version) )
			throw "Can't init game server";

		log("Server started");

		steam.GameServer.setConfig(config.name, config.name, config.description);
		steam.GameServer.setInfo(4, false, config.name, 0, "DefaultMap");

		steam.GameServer.logonAnonymous(function(ok) {
			if( !ok ) {
				log("Connection failed");
				Sys.exit(1);
			}
			log("LOGIN OK");
			steam.GameServer.enableHeartbeats(true);
			var id = steam.GameServer.getSteamID();
			log("UID = " + id.toString());
			var ip = steam.GameServer.getPublicIP();
			log("IP = " + (ip >>> 24) + "." + ((ip >> 16) & 0xFF) + "." + ((ip >> 8) & 0xFF) + "." + (ip & 0xFF));

			sys.io.File.saveContent("server.conf", haxe.Serializer.run({ id : id.getBytes() }));


			steam.Networking.startP2P({
				onData : function(user, data) {
					trace(user, data);
					steam.Networking.sendP2P(user, haxe.io.Bytes.ofString("Ping!"), Reliable);
				},
				onConnectionRequest : function(user) { trace(user); return true; },
				onConnectionError : function(user,status) trace(user, status),
			});

		});
	}

}


class GameClient {

	public static function main() {
		var app = Std.parseInt(sys.io.File.getContent("steam_appid.txt"));
		if( !steam.Api.init(app) )
			throw "Can't init steam client";
		log("Client connected");

		var serverInfos : { id : haxe.io.Bytes } = haxe.Unserializer.run(sys.io.File.getContent("server.conf"));

		trace(serverInfos.id.toHex());

		steam.Networking.startP2P({
			onData : function(user, data) trace(user, data),
			onConnectionRequest : function(user) { trace(user); return true; },
			onConnectionError : function(user,status) trace(user, status),
		});

		var server = steam.User.fromUID(steam.UID.fromBytes(serverInfos.id));
		steam.Networking.sendP2P(server, haxe.io.Bytes.ofString("Hello!"), Reliable);

		/*
		var config : Dynamic = haxe.Json.parse(sys.io.File.getContent("config.json"));
		steam.GameServer.requestInternetServerList(steam.Api.appId, {}, function(list) {
			Sys.println("Servers found: " + list);
			if( list.length == 0 ) {
				Sys.exit(0);
				return;
			}
			var cnx = list[0];
			trace(cnx.id.toString());
		});*/
	}

}
