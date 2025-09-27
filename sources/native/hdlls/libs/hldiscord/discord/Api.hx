package discord;

@:hlNative("discord")
class Api{

    public inline static function updatePresence(state : String, details : String, autoUpdate : Bool) {
        update_state(@:privateAccess state.toUtf8(), false);
        update_details(@:privateAccess details.toUtf8(), autoUpdate);
    }

    public static function init(discordAppID : String, steamAppID : String){
        initApi(@:privateAccess discordAppID.toUtf8(), @:privateAccess steamAppID.toUtf8());
    }

    public inline static function updateState(value : String, autoUpdate = true) {
        update_state(@:privateAccess value.toUtf8(), autoUpdate);
    }

    public inline static function updateDetails(value : String, autoUpdate = true) {
        update_details(@:privateAccess value.toUtf8(), autoUpdate);
    }

    public inline static function updateLargeImageKey(value : String, autoUpdate = true) {
        update_largeImageKey(@:privateAccess value.toUtf8(), autoUpdate);
    }

    public inline static function updateLargeImageText(value : String, autoUpdate = true) {
        update_largeImageText(@:privateAccess value.toUtf8(), autoUpdate);
    }

    public inline static function updateSmallImageKey(value : String, autoUpdate = true) {
        update_smallImageKey(@:privateAccess value.toUtf8(), autoUpdate);
    }

    public inline static function updateSmallImageText(value : String, autoUpdate = true) {
        update_smallImageText(@:privateAccess value.toUtf8(), autoUpdate);
    }

    public inline static function updateMatchSecret(value : String, autoUpdate = true) {
        update_matchSecret(@:privateAccess value.toUtf8(), autoUpdate);
    }

    public inline static function updateJoinSecret(value : String, autoUpdate = true) {
        update_joinSecret(@:privateAccess value.toUtf8(), autoUpdate);
    }

    public inline static function updateSpectateSecret(value : String, autoUpdate = true) {
        update_spectateSecret(@:privateAccess value.toUtf8(), autoUpdate);
    }

    public inline static function updatePartySize(value : Int, autoUpdate = true) {
        update_partySize(value, autoUpdate);
    }

    public inline static function updatePartyMax(value : Int, autoUpdate = true) {
        update_partyMax(value, autoUpdate);
    }

    public inline static function updateStartTimestamp(value : Int, autoUpdate = true) {
        update_startTimestamp(value, autoUpdate);
    }

    public inline static function updateEndTimestamp(value : Int, autoUpdate = true) {
        update_endTimestamp(value, autoUpdate);
    }

    @:hlNative("discord", "release")
    public static function release(){
    }
                           
    @:hlNative("discord", "update_partySize")
    static function update_partySize(value : Int, autoUpdate : Bool) {}

    @:hlNative("discord", "update_partyMax")
    static function update_partyMax(value : Int, autoUpdate : Bool) {}

    @:hlNative("discord", "update_startTimestamp")
    static function update_startTimestamp(value : Int, autoUpdate : Bool) {}

    @:hlNative("discord", "update_endTimestamp")
    static function update_endTimestamp(value : Int, autoUpdate : Bool) {}

    @:hlNative("discord", "update_state")
    static function update_state(value : hl.Bytes, autoUpdate : Bool) {}

    @:hlNative("discord", "update_details")
    static function update_details(value : hl.Bytes, autoUpdate : Bool) {}

    @:hlNative("discord", "update_largeImageKey")
    static function update_largeImageKey(value : hl.Bytes, autoUpdate : Bool) {}

    @:hlNative("discord", "update_largeImageText")
    static function update_largeImageText(value : hl.Bytes, autoUpdate : Bool) {}

    @:hlNative("discord", "update_smallImageKey")
    static function update_smallImageKey(value : hl.Bytes, autoUpdate : Bool) {}

    @:hlNative("discord", "update_smallImageText")
    static function update_smallImageText(value : hl.Bytes, autoUpdate : Bool) {}

    @:hlNative("discord", "update_matchSecret")
    static function update_matchSecret(value : hl.Bytes, autoUpdate : Bool) {}

    @:hlNative("discord", "update_joinSecret")
    static function update_joinSecret(value : hl.Bytes, autoUpdate : Bool) {}

    @:hlNative("discord", "update_spectateSecret")
    static function update_spectateSecret(value : hl.Bytes, autoUpdate : Bool) {}

    @:hlNative("discord", "init")
    static function initApi(discordAppID: hl.Bytes, steamAppID : hl.Bytes) {
    }
}