package steam;

enum abstract ChatMessageType(Int) {
	var Invalid = 0;
	/** Normal text message from another user **/
	var ChatMsg = 1;
	/** Another user is typing (not used in multi-user chat) **/
	var Typing = 2;
	/** Invite from other user into that users current game **/
	var InviteGame = 3;
	/** user has left the conversation ( closed chat window ) **/
	var LeftConversation = 6;
	/** user has entered the conversation (used in multi-user chat and group chat) **/
	var Entered = 7;
	/** user was kicked (data: 64-bit steamid of actor performing the kick) **/
	var WasKicked = 8;
	/** user was banned (data: 64-bit steamid of actor performing the ban) **/
	var WasBanned = 9;
	/** user disconnected **/
	var Disconnected = 10;
	/** a chat message from user's chat history or offilne message **/
	var HistoricalChat = 11;
	/** a link was removed by the chat filter **/
	var LinkBlocked = 14;
}

@:hlNative("steam")
@:access(String)
class Lobby {

	public var uid(default, null) : UID;
	public var owner(get, never) : User;
	public var maxMembers(get, set) : Int;

	public function new(uid:UID) {
		this.uid = uid;
	}

	public function set( key : String, value : Dynamic ) {
		setRawData(key, haxe.Serializer.run(value));
	}

	public function setRawData( key : String, value : String ) {
		if( !set_lobby_data(uid, key.toUtf8(), value.toUtf8()) )
			throw "Failed to set lobby data";
	}

	public function remove( key : String ) {
		return delete_lobby_data(uid, key.toUtf8());
	}

	public function get( key : String ) : Dynamic {
		var str = getRawData(key);
		return str == null || str.length == 0 ? null : haxe.Unserializer.run(str);
	}

	public function getRawData( key : String ) {
		var v = get_lobby_data(uid, key.toUtf8());
		return v == null ? null : String.fromUTF8(v);
	}

	public function getAllRaw( maxSize = 65536 ) : Map<String,String> {
		var m = new Map();
		var tmpKey = new hl.Bytes(256);
		var tmpData = new hl.Bytes(maxSize);
		for( i in 0...get_lobby_data_count(uid) ) {
			if( !get_lobby_data_byindex(uid, i, tmpKey, 256, tmpData, maxSize) )
				throw "Too much data";
			m.set(String.fromUTF8(tmpKey), String.fromUTF8(tmpData));
		}
		return m;
	}

	public function getAll( maxSize = 65536 ) : Map<String,Dynamic> {
		var m = getAllRaw(maxSize);
		var out = new Map();
		for( k in m.keys() ) {
			var value = m.get(k);
			if( value.length > 0 )
				try {
					out.set(k, haxe.Unserializer.run(value));
				} catch( e : Dynamic ) {
					// ignore
				}
		}
		return m;
	}

	public function leave() {
		dispose();
		leave_lobby(uid);
	}

	function get_owner() {
		return User.fromUID(get_lobby_owner(uid));
	}

	public function join( onJoin : { inviteOnly : Bool } -> Void ) {
		return join_lobby(uid, function(v, error) { if( !error ) register(); onJoin(error ? null : { inviteOnly : v }); });
	}

	public function getMembers() {
		return [for( i in 0...get_num_lobby_members(uid) ) User.fromUID(get_lobby_member_by_index(uid, i))];
	}

	public function getMemberData( user : User, key : String ) : String {
		var v = get_lobby_member_data(uid, user.uid, key.toUtf8());
		return v == null ? null : String.fromUTF8(v);
	}

	public function setMemberData( key : String, value : String ) {
		set_lobby_member_data(uid, key.toUtf8(), value.toUtf8());
	}

	public function requestData() {
		register();
		return request_lobby_data(uid);
	}

	function register() {
		@:privateAccess Matchmaking.lobbies.set(uid.toString(), this);
	}

	public dynamic function onDataUpdated() {
	}

	public dynamic function onUserDataUpdated( user : User ) {
	}

	public dynamic function onUserJoined( user : User ) {
	}

	public dynamic function onUserLeft( user : User ) {
	}

	public dynamic function onChatMessage( type : ChatMessageType, cid : Int ) {
	}

	public function getChatEntry( cid : Int, maxData = 1024 ) : { user : User, data : haxe.io.Bytes, type : ChatMessageType } {
		var tmp = new hl.Bytes(maxData);
		var type = Invalid;
		var len = 0;
		var user = get_lobby_chat_entry(uid, cid, tmp, maxData, type, len);
		return { user : User.fromUID(user), data : tmp.sub(0, len).toBytes(len), type : type };
	}

	public function sendChatMessage( bytes : haxe.io.Bytes ) {
		if( !send_lobby_chat_msg(uid, bytes, bytes.length) )
			throw "Failed to send chat message";
	}

	public function inviteFriends() {
		lobby_invite_friends(uid);
	}

	public function inviteUser( user : User ) {
		return lobby_invite_user(uid, user.uid);
	}

	public function toString() {
		return "Lobby#" + uid.toString();
	}

	public function dispose() {
		onDataUpdated = function() {};
		onUserDataUpdated = onUserJoined = onUserLeft = function(_) {};
		onChatMessage = function(_, _) {};
		@:privateAccess Matchmaking.lobbies.remove(uid.toString());
	}

	function get_maxMembers() {
		return get_lobby_member_limit(uid);
	}

	function set_maxMembers(v) {
		set_lobby_member_limit(uid, v);
		return v;
	}

	// --- HL stubs ----

	static function request_lobby_data( uid : UID ) {
		return false;
	}

	static function set_lobby_data( uid : UID, key : hl.Bytes, data : hl.Bytes ) {
		return false;
	}

	static function delete_lobby_data( uid : UID, key : hl.Bytes ) {
		return false;
	}

	static function get_lobby_data( uid : UID, key : hl.Bytes ) : hl.Bytes {
		return null;
	}

	static function get_lobby_data_count( uid : UID ) : Int {
		return 0;
	}

	static function get_lobby_data_byindex( uid : UID, index : Int, key : hl.Bytes, keySize : Int, data : hl.Bytes, dataSize : Int ) {
		return false;
	}

	static function leave_lobby( uid : UID ) {
	}

	static function join_lobby( uid : UID, onJoin : Callback<Bool> ) : AsyncCall {
		return null;
	}

	static function get_num_lobby_members( uid : UID ) : Int {
		return 0;
	}

	static function get_lobby_member_by_index( uid : UID, index : Int ) : UID {
		return null;
	}

	static function get_lobby_owner( uid : UID ) : UID {
		return null;
	}

	static function lobby_invite_friends( uid : UID ) {
	}

	static function send_lobby_chat_msg( uid : UID, bytes : hl.Bytes, len : Int ) : Bool {
		return false;
	}

	static function get_lobby_chat_entry( uid : UID, cid : Int, tmp : hl.Bytes, tmpLen : Int, type : hl.Ref<ChatMessageType>, len : hl.Ref<Int> ) : UID {
		return null;
	}

	static function lobby_invite_user( uid : UID, fid : UID ) {
		return false;
	}

	static function get_lobby_member_data( lid : UID, uid : UID, key : hl.Bytes ) : hl.Bytes {
		return null;
	}

	static function set_lobby_member_data( lid : UID, key : hl.Bytes, value : hl.Bytes ) {
	}

	static function get_lobby_member_limit( uid : UID ) {
		return 0;
	}

	static function set_lobby_member_limit( uid : UID, count : Int ) {
	}

}