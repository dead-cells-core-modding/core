package steam.ugc;
import steam.UID;

enum ItemState {
	Subscribed;
	LegacyItem;
	Installed;
	NeedsUpdate;
	Downloading;
	DownloadPending;
}

@:hlNative("steam")
class Item {

	public var id : UID;
	public static var downloadedCallbacks = new Array<Item->Void>();
	public static var installedCallbacks = new Array<Item->Void>();

	public static function fromInt( i : Int ){
		var b = haxe.io.Bytes.alloc(8);
		b.setInt32(0, i);
		return new Item(steam.UID.fromBytes(b));
	}

	public static function init( onDownloaded : Item -> Void, onInstalled : Item -> Void ){
		downloadedCallbacks.push(onDownloaded);
		installedCallbacks.push(onInstalled);
		Api.registerGlobalEvent(3400 + 6, function(data:{file:UID}){
			var item = new Item(data.file);
			for( callback in downloadedCallbacks ){
				callback(item);
			}
		});
		Api.registerGlobalEvent(3400 + 5, function(data:{file:UID}){
			var item = new Item(data.file);
			for( callback in installedCallbacks ){
				callback(item);
			}
		});
	}

	public static function create( appId : Int, cb : Null<Item> -> Bool -> Void ){
		ugc_item_create(appId,function(obj, error){
			if( error ){
				cb(null,false);
				return;
			}
			cb(new Item(obj.id), obj.userNeedsLegalAgreement);
		});
	}

	public static function listSubscribed() : Array<Item> {
		var a = get_subscribed_items();
		return a == null ? null : [for( it in a ) new Item(it)];
	}

	inline function new( b : UID ){
		id = b;
	}

	public function getState() : haxe.EnumFlags<ItemState> {
		return haxe.EnumFlags.ofInt( get_item_state(id) );
	}

	public function getDownloadInfo() : {downloaded: Float, total: Float} {
		var downloaded = 0.;
		var total = 0.;

		if( !get_item_download_info(id, downloaded, total) )
			return null;
		return {
			downloaded: downloaded,
			total: total
		};
	}

	public function getInstallInfo(){
		var r = get_item_install_info(id);
		if( r == null ) return null;
		return {
			size: r.size,
			timestamp: r.time,
			path: @:privateAccess String.fromUTF8(r.path)
		};
	}

	public function download( highPriority : Bool ) : Bool {
		return download_item( id, highPriority );
	}

	public function subscribe( cb ){
		subscribe_item( id, cb );
	}

	public function unsubscribe( cb ){
		unsubscribe_item( id, cb );
	}

	public function toString(){
		return 'UGCItem($id)';
	}
	
	public function delete( cb : Int -> Bool -> Void ) {
		return delete_item(id, cb);
	}
	
	public function addAppDependency( appId : Int, cb : Int -> Bool -> Void ) {
		return add_app_dependency(id, appId, cb);
	}

	public function removeAppDependency( appId : Int, cb : Int -> Bool -> Void ) {
		return remove_app_dependency(id, appId, cb);
	}

	public function getAppDependencies( cb : { result : Int, deps : hl.NativeArray<Int> } -> Bool -> Void ) {
		return get_app_dependencies(id, cb);
	}
	

	// -- native

	static function ugc_item_create( appId : Int, cb : Callback<Dynamic> ) : AsyncCall {
		return null;
	}
	
	static function get_subscribed_items() : hl.NativeArray<UID> {
		return null;
	}

	static function get_item_state( item : UID ) : Int {
		return 0;
	}

	static function get_item_download_info( item : UID, downloaded : hl.Ref<hl.F64>, total : hl.Ref<hl.F64> ) : Bool {
		return false;
	}

	static function download_item( item : UID, hPriority: Bool ) : Bool {
		return false;
	}

	static function get_item_install_info( item : UID ) : Dynamic {
		return null;
	}
	
	static function subscribe_item( item : UID, cb : Callback<UID> ) : AsyncCall {
		return null;
	}

	static function unsubscribe_item( item : UID, cb : Callback<UID> ) : AsyncCall {
		return null;
	}

	static function delete_item( item : UID, cb : Callback<Int> ) : AsyncCall {
		return null;
	}

	static function add_app_dependency( item : UID, appId : Int, cb : Callback<Int> ) : AsyncCall {
		return null;
	}

	static function remove_app_dependency( item : UID, appId : Int, cb : Callback<Int> ) : AsyncCall {
		return null;
	}
	
	static function get_app_dependencies( item : UID, cb : Callback<Dynamic> ) : AsyncCall {
		return null;
	}

}

