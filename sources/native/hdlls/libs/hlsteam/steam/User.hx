package steam;

enum Changed {
	Name;
	Status;
	ComeOnline;
	GoneOffline;
	GamePlayed;
	GameServer;
	Avatar;
	JoinedSource;
	LeftSource;
	RelationshipChanged;
	NameFirstSet;
	FacebookInfo;
	Nickname;
	SteamLevel;
}

enum AvatarSize {
	Small;
	Medium;
	Large;
}

@:hlNative("steam")
class User {

	public var uid(default, null) : UID;
	public var name(get, never) : String;
	var cachedName : String;
	var waiting : Array<haxe.EnumFlags<Changed>->Void>;
	var p2pcnx : Bool;

	function new(uid) {
		this.uid = uid;
		waiting = [];
	}

	public function getID32() {
		// lower 32 bits seems to be unique, then padded with 0x01100001
		return (cast uid : hl.Bytes).getI32(0);
	}

	function get_name() {
		if( cachedName != null ) return cachedName;
		cachedName = @:privateAccess String.fromUTF8(get_user_name(uid));
		return cachedName;
	}

	public function requestInformation( onChange, nameOnly = false ) {
		waiting.push(onChange);
		request_user_information(uid, nameOnly);
	}

	public function toString() {
		return name+"(" + uid.toString() + ")";
	}

	#if heaps
	public function getAvatarImage(size) : h2d.Tile {
		var data = getAvatar(size);
		if( data == null )
			return h2d.Tile.fromColor(0, 32, 32);
		return h2d.Tile.fromPixels(new hxd.Pixels(data.width, data.height, data.rgba, RGBA));
	}
	#end

	public function getAvatar( size : AvatarSize ) : { width : Int, height : Int, rgba : haxe.io.Bytes } {
		var w = 0;
		var h = 0;
		var bytes = get_user_avatar(uid, size.getIndex(), w, h);
		if( bytes == null ) return null;
		return { width : w, height : h, rgba : bytes.toBytes(w * h * 4) };
	}

	function onDataUpdated(flags) {
		if( waiting.length > 0 ) {
			var old = waiting;
			waiting = [];
			for( w in old ) w(flags);
		}
	}

	static function get_user_name( uid : UID ) : hl.Bytes {
		return null;
	}

	// this will most likely leak but we don't care
	static var users = new Map<String, User>();

	public static function fromUID( uid : UID ) {
		var u = users.get(uid.toString());
		if( u != null ) return u;
		u = new User(uid);
		users.set(uid.toString(), u);
		return u;
	}

	public static function fromUID32( uid : Int ) {
		var bytes = new hl.Bytes(8);
		bytes.setI32(0, uid);
		bytes.setI32(4, 0x01100001);
		return fromUID(cast bytes);
	}

	static function request_user_information( uid : UID, nameOnly : Bool ) {
		return false;
	}

	static function get_user_avatar( uid : UID, size : Int, width : hl.Ref<Int>, height : hl.Ref<Int> ) : hl.Bytes {
		return null;
	}

}