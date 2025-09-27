package steam.ugc;
import steam.ugc.Data;

@:hlNative("steam")
class ItemUpdate {

	var id : UID;

	public static function start( appId : Int, item : UID ) : ItemUpdate{
		var updId = ugc_item_start_update(appId, item);
		if( updId == null ) return null;
		return new ItemUpdate(updId);
	}

	function new( updId : UID ){
		id = updId;
	}

	public function submit( notes : String, cb : Bool -> Dynamic -> Void ){
		return ugc_item_submit_update(id,notes == null ? null : @:privateAccess notes.toUtf8(), function(needsAgreement,error) cb(!error,needsAgreement));
	}

	public function setUpdateLanguage( lang : String ){
		return ugc_item_set_update_language(id, @:privateAccess lang.toUtf8());
	}

	public function setTitle( title : String ){
		return ugc_item_set_title(id, @:privateAccess title.toUtf8());
	}

	public function setDescription( desc : String ){
		return ugc_item_set_description(id, @:privateAccess desc.toUtf8());
	}

	public function setMetadata( metadata : String ){
		return ugc_item_set_metadata(id, @:privateAccess metadata.toUtf8());
	}

	public function setTags( tags : Array<String> ) : Bool {
		var a = new hl.NativeArray(tags.length);
		for( i in 0...tags.length )
			a[i] = @:privateAccess tags[i].toUtf8();
		return ugc_item_set_tags(id, a);
	}

	public function addKeyValueTag( key : String, value : String ){
		return @:privateAccess ugc_item_add_key_value_tag(id, key.toUtf8(), value.toUtf8());
	}

	public function removeKeyValueTag( key : String ){
		return ugc_item_remove_key_value_tags(id, @:privateAccess key.toUtf8());
	}

	public function setVisibility( visibility : EItemVisibility ){
		return ugc_item_set_visibility(id, visibility);
	}

	public function setContent( path : String ){
		return ugc_item_set_content(id, @:privateAccess path.toUtf8());
	}

	public function setPreviewImage( path : String ){
		return ugc_item_set_preview_image(id, @:privateAccess path.toUtf8());
	}

	// -- native
	
	static function ugc_item_start_update( appId : Int, itemId : UID ) : UID { return null; }
	static function ugc_item_submit_update( updId : UID, notes : hl.Bytes, cb : Callback<Bool> ) : AsyncCall { return null; }
	static function ugc_item_set_update_language( updId : UID, lang : hl.Bytes ) : Bool { return false; }
	static function ugc_item_set_title( updId : UID, title : hl.Bytes ) : Bool { return false; }
	static function ugc_item_set_description( updId : UID, desc : hl.Bytes ) : Bool { return false; }
	static function ugc_item_set_metadata( updId : UID, metadata : hl.Bytes ) : Bool { return false; }
	static function ugc_item_set_tags( updId : UID, tags : hl.NativeArray<hl.Bytes> ) : Bool { return false; }
	static function ugc_item_add_key_value_tag( updId : UID, key : hl.Bytes, value : hl.Bytes ) : Bool { return false; }
	static function ugc_item_remove_key_value_tags( updId : UID, key : hl.Bytes ) : Bool { return false; }
	static function ugc_item_set_visibility( updId : UID, vis : Int ) : Bool { return false; }
	static function ugc_item_set_content( updId : UID, path : hl.Bytes ) : Bool { return false; }
	static function ugc_item_set_preview_image( updId : UID, path : hl.Bytes ) : Bool { return false; }

}