package steam;

private typedef AsyncData = hl.Abstract<"steam_call_result">;

@:hlNative("steam")
abstract AsyncCall(AsyncData) {

	public inline function cancel() {
		if( this != null ) {
			cancelCallResult(this);
			this = null;
		}
	}

	static function cancelCallResult( a : AsyncData ) {
	}

}
