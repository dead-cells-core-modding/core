package steam.ugc;

import steam.UID;
import steam.ugc.Data;

class Result {

	public var id : UID;
	public var item(get,never) : Item;
	public var result : EResult;
	public var fileType : EFileType;
	public var creatorApp : Int;
	public var consumerApp : Int;
	public var title : String;
	public var description : String;
	public var owner : User;
	public var created : Int; // Date ?
	public var updated : Int;
	public var timeAddedToUserList : Int;
	public var visibility : EItemVisibility;
	public var banned : Bool;
	public var acceptedForUse : Bool;
	public var tags : Array<String>;
	public var previewFile : UID;
	public var previewFileSize : Int;
	public var file : UID;
	public var fileName : String;
	public var fileSize : Int;
	public var url : String;
	public var votesUp : Int;
	public var votesDown : Int;
	public var score : Float;
	public var numChildren : Int;
	public var keyValueTags : Array<{key: String, value: String}>;
	public var metadata : String;
	public var children : Array<steam.ugc.Item>;

	function new( obj : Dynamic ){
		id = obj.id;
		result = obj.result;
		fileType = obj.fileType;
		creatorApp = obj.creatorApp;
		consumerApp = obj.consumerApp;
		title = @:privateAccess String.fromUTF8(obj.title);
		description = @:privateAccess String.fromUTF8(obj.description);
		owner = User.fromUID(obj.owner);
		created = obj.timeCreated;
		updated = obj.timeUpdated;
		timeAddedToUserList = obj.timeAddedToUserList;
		visibility = obj.visibility;
		banned = obj.banned;
		acceptedForUse = obj.acceptedForUse;
		tags = @:privateAccess String.fromUTF8(obj.tags).split(",");
		previewFile = obj.previewFile;
		previewFileSize = obj.previewFileSize;
		file = obj.file;
		fileName = @:privateAccess String.fromUTF8(obj.fileName);
		fileSize = obj.fileSize;
		url = @:privateAccess String.fromUTF8(obj.url);
		votesUp = obj.votesUp;
		votesDown = obj.votesDown;
		score = obj.score;
		numChildren = obj.numChildren;
		children = [];
	}

	inline public function get_item(){
		return @:privateAccess new Item(id);
	}

	public function getKeyValueTag( key : String ) : String {
		for( o in keyValueTags )
			if( o.key == key ) return o.value;
		return null;
	}
}

@:hlNative("steam")
class Query {

	var id : UID;
	public var returnKeyValueTags(default,set) : Bool;
	public var returnMetadata(default,set) : Bool;
	public var returnChildren(default,set) : Bool;

	public var sent(default,null) : Bool = false;
	public var resultsReturned(default,null) : Int;
	public var totalResults(default,null) : Int;
	public var cached(default,null) : Bool;

	public static function list( type : EQueryType, ugcType : EMatchingType, creatorApp : Int, consumerApp : Int, page : Int ) : Query {
		var id = ugc_query_create_all_request(type,ugcType,creatorApp,consumerApp,page);
		if( id == null ) return null;
		return new Query(id);
	}

	public static function userList( user : steam.User, listType : EUserList, ugcType : EMatchingType, order : EUserListSortOrder, creatorApp : Int, consumerApp : Int, page : Int ){
		var id = ugc_query_create_user_request(user.getID32(),listType,ugcType,order,creatorApp,consumerApp,page);
		if( id == null ) return null;
		return new Query(id);
	}

	public static function details( ids : Array<Item> ) : Query {
		var a = new hl.NativeArray(ids.length);
		for( i in 0...ids.length )
			a[i] = ids[i].id;
		var id = ugc_query_create_details_request(a);
		if( id == null ) return null;
		return new Query(id);
	}

	function new( b : UID ){
		id = b;
	}

	public function setLanguage( lang : String ){
		ugc_query_set_language(id,@:privateAccess lang.toUtf8());
	}

	public function setSearchText( search : String ){
		ugc_query_set_search_text(id,@:privateAccess search.toUtf8());
	}

	public function addRequiredTag( tag : String ){
		ugc_query_add_required_tag(id,@:privateAccess tag.toUtf8());
	}

	public function addRequiredKeyValueTag( key : String, value : String ){
		ugc_query_add_required_key_value_tag(id,@:privateAccess key.toUtf8(), @:privateAccess value.toUtf8());
	}

	public function addExcludedTag( tag : String ){
		ugc_query_add_excluded_tag(id,@:privateAccess tag.toUtf8());
	}

	public function set_returnMetadata( b : Bool ){
		ugc_query_set_return_metadata(id,b);
		return returnMetadata = b;
	}

	public function set_returnKeyValueTags( b : Bool ){
		ugc_query_set_return_key_value_tags(id,b);
		return returnKeyValueTags = b;
	}

	public function set_returnChildren( b : Bool ){
		ugc_query_set_return_children(id,b);
		return returnChildren = b;
	}

	public function send( cb : Bool -> Void ){
		sent = true;
		ugc_query_send_request(id,function(obj,error){
			if( !error ){
				resultsReturned = obj.resultsReturned;
				totalResults = obj.totalResults;
				cached = obj.cached;
			}
			cb(!error);
		});
	}

	public function close(){
		ugc_query_release_request(id);
	}

	public function getResult( index : Int ){
		var r = ugc_query_get_result(id,index);
		if( r == null ) return null;
		var res = @:privateAccess new Result(r);
		
		if( returnKeyValueTags ){
			var ntags = ugc_query_get_key_value_tags(id,index,0xFFFF);
			res.keyValueTags = [];
			var i = 0;
			while( i < ntags.length ) @:privateAccess {
				res.keyValueTags.push({
					key: String.fromUTF8( ntags[i++] ),
					value: String.fromUTF8( ntags[i++] )
				});
			}
		}

		if( returnMetadata ){
			var s = ugc_query_get_metadata(id,index);
			if( s != null ) res.metadata = @:privateAccess String.fromUTF8(s);
		}

		if( returnChildren && res.numChildren > 0 ){
			var r = ugc_query_get_children(id,index,res.numChildren);
			if( r != null ) res.children = [for( uid in r ) @:privateAccess new Item(uid)];
		}

		return res;
	}

	// -- native

	static function ugc_query_create_all_request( type : Int, ugcType : Int, creatorApp : Int, consumerApp : Int, page : Int ) : UID { return null; }
	static function ugc_query_create_user_request( user : Int, listType : Int, ugcType : Int, sortOrder : Int, creatorApp : Int, consumerApp : Int, page : Int ) : UID { return null; }
	static function ugc_query_create_details_request( fileIds : hl.NativeArray<UID> ) : UID { return null; }
	static function ugc_query_set_language( query : UID, lang : hl.Bytes ) : Bool { return false; }
	static function ugc_query_set_search_text( query : UID, search : hl.Bytes ) : Bool { return false; }
	static function ugc_query_add_required_tag( query : UID, tag : hl.Bytes ) : Bool { return false; }
	static function ugc_query_add_required_key_value_tag( query : UID, key : hl.Bytes, value : hl.Bytes ) : Bool { return false; }
	static function ugc_query_add_excluded_tag( query : UID, tag : hl.Bytes ) : Bool { return false; }
	static function ugc_query_set_return_metadata( query : UID, r : Bool ) : Bool { return false; }
	static function ugc_query_set_return_children( query : UID, r : Bool ) : Bool { return false; }
	static function ugc_query_set_return_key_value_tags( query : UID, r : Bool ) : Bool { return false; }
	static function ugc_query_send_request( query : UID, cb : Callback<Dynamic> ) : AsyncCall { return null; }
	static function ugc_query_release_request( query : UID ) : Bool { return false; }
	static function ugc_query_get_key_value_tags( query : UID, index : Int, maxValueLength : Int ) : hl.NativeArray<hl.Bytes> { return null; }
	static function ugc_query_get_children( query : UID, index : Int, numChildren : Int ) : hl.NativeArray<UID> { return null; }
	static function ugc_query_get_metadata( query : UID, index : Int ) : hl.Bytes { return null; }
	static function ugc_query_get_result( query : UID, index : Int ) : Dynamic { return null; }


}