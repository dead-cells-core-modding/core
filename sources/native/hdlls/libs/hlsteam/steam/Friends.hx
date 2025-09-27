package steam;

enum abstract FriendFlags(Int) {
	var None = 0;
	var Blocked = 1;
	var FriendshipRequested = 2;
	var Immediate = 4;
	var ClanMember = 8;
	var OnGameServer = 0x10;
	var RequestingFrienship = 0x80;
	var RequestingInfo = 0x100;
	var Ignored = 0x200;
	var IgnoredFriend = 0x400;
	var ChatMember = 0x1000;
	var All = 0xFFFF;
	@:op(a | b) static function or(a:FriendFlags, b:FriendFlags):FriendFlags;
}

enum abstract OverlayKind(String) {

	var None = "none";

	var Friends = "Friends";
	var Community = "Community";
	var Players = "Players";
	var Settings = "Settings";
	var OfficialGameGroup = "OfficialGameGroup";
	var Stats = "Stats";
	var Achievements = "Achievements";

	// with uid

	/**
		opens the overlay web browser to the specified user or groups profile
	**/
	var SteamID = "steamid";
	/**
		opens a chat window to the specified user, or joins the group chat
	**/
	var Chat = "chat";
	/**
		opens a window to a Steam Trading session that was started with the ISteamEconomy/StartTrade Web API
	**/
	var JoinTrade = "jointrade";
	/**
		opens the overlay web browser to the specified user's stats
	**/
	var UserStats = "stats";
	/**
		opens the overlay web browser to the specified user's achievements
	**/
	var UserAchievements = "achievements";
	/**
		opens the overlay in minimal mode prompting the user to add the target user as a friend
	**/
	var FriendAdd = "friendadd";
	/**
		opens the overlay in minimal mode prompting the user to remove the target friend
	**/
	var FriendRemove = "friendremove";
	/**
		opens the overlay in minimal mode prompting the user to accept an incoming friend invite
	**/
	var FriendRequestAccept = "friendrequestaccept";
	/**
		opens the overlay in minimal mode prompting the user to ignore an incoming friend invite
	**/
	var FriendRequestIgnore = "friendrequestignore";

	public function toString() return this;
}

enum abstract OverlayToStoreFlag(Int) {
	var None = 0;
	var AddToCart = 1;
	var AddToCartAndShow = 2;
}

@:hlNative("steam")
class Friends {

	public static function getFriends( ?flags : FriendFlags ) {
		if( flags == null ) flags = Immediate;
		return [for( uid in get_friends(flags) ) User.fromUID(uid)];
	}

	public static function hasFriend( user : User, ?flags : FriendFlags ) {
		if( flags == null ) flags = Immediate;
		return has_friend(user.uid, flags);
	}

	public static function activateOverlay( overlay : OverlayKind, ?uid : UID ) {
		activate_overlay_user( overlay == None ? null : @:privateAccess overlay.toString().toUtf8(), uid);
	}
	
	public static function activateOverlayStore( appId : Int, flags : OverlayToStoreFlag ) : Void {}

	static function get_friends( flags : FriendFlags ) : hl.NativeArray<UID> { return null; }
	static function has_friend( uid : UID, flags : FriendFlags ) : Bool { return false; }
	static function activate_overlay_user( overlay : hl.Bytes, uid : UID ) : Void {}

}