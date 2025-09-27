package steam;

abstract UID(hl.Bytes) {
	function new(uid) {
		this = uid;
	}
	public function toString() {
		return this == null ? "NULL" : getBytes().toHex();
	}
	public function getBytes() {
		return this.toBytes(8);
	}
	public function getInt64() : haxe.Int64 {
		return haxe.Int64.make(this.getI32(4), this.getI32(0));
	}
	@:op(a == b) static function __compare( a : UID, b : UID ) {
		return (cast a : hl.Bytes).compare(0, (cast b : hl.Bytes), 0, 8) == 0;
	}
	public static function fromBytes( bytes : haxe.io.Bytes ) : UID {
		if( bytes.length != 8 ) throw "Invalid UID";
		return new UID(@:privateAccess bytes.b);
	}
}