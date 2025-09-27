package steam;
import steam.helpers.MacroHelper;

/**
 * The Steam Controller API. Used by API.hx, should never be created manually by the user.
 * API.hx creates and initializes this by default.
 * Access it via API.controller static variable
 */

@:allow(steam.Api)
class Controller
{
	/**
	 * The maximum number of controllers steam can recognize. Use this for array upper bounds.
	 */
	public var MAX_CONTROLLERS(get, null):Int;

	/**
	 * The maximum number of analog actions steam can recognize. Use this for array upper bounds.
	 */
	public var MAX_ANALOG_ACTIONS(get, null):Int;

	/**
	 * The maximum number of digital actions steam can recognize. Use this for array upper bounds.
	 */
	public var MAX_DIGITAL_ACTIONS(get, null):Int;

	/**
	 * The maximum number of origins steam can assign to one action. Use this for array upper bounds.
	 */
	public var MAX_ORIGINS(get, null):Int;

	/**
	 * The maximum value steam will report for an analog action.
	 */
	public var MAX_ANALOG_VALUE(get, null):Float;

	/**
	 * The minimum value steam will report for an analog action.
	 */
	public var MIN_ANALOG_VALUE(get, null):Float;

	public static inline var MAX_SINGLE_PULSE_TIME:Int = 65535;

	/*************PUBLIC***************/

	/**
	 * Whether the controller API is initialized or not. If false, all calls will fail.
	 */
	public var active(default, null):Bool = false;

	/**
	 * Reconfigure the controller to use the specified action set (ie 'Menu', 'Walk' or 'Drive')
	 * This is cheap, and can be safely called repeatedly. It's often easier to repeatedly call it in
	 * your state loops, instead of trying to place it in all of your state transitions.
	 *
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	actionSet	handle received from getActionSetHandle()
	 * @return	1 = success, 0 = failure
	 */
	public function activateActionSet(controller:Int, actionSet:Int):Void {
		if (!active) return;
		_ActivateActionSet(controller, actionSet);
	}

	/**
	 * Get the handle of the current action set
	 *
	 * @param	controller	handle received from getConnectedControllers()
	 * @return	handle of the current action set
	 */
	public function getCurrentActionSet(controller:Int):Int {
		if (!active) return -1;
		return _GetCurrentActionSet(controller);
	}

	/**
	 * Lookup the handle for an Action Set. Best to do this once on startup, and store the handles for all future API calls.
	 *
	 * @param	name identifier for the action set specified in your vdf file (ie, 'Menu', 'Walk' or 'Drive')
	 * @return	action set handle
	 */
	public function getActionSetHandle(name:String):Int {
		if (!active) return -1;
		return _GetActionSetHandle(@:privateAccess name.toUtf8());
	}

	/**
	 * Returns the current state of the supplied analog game action
	 *
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	action	handle received from getActionSetHandle()
	 * @param	data	existing ControllerAnalogActionData structure you want to fill (optional)
	 * @return	data structure containing analog x,y values & other data
	 */
	public function getAnalogActionData(controller:Int, action:Int, ?data:ControllerAnalogActionData):ControllerAnalogActionData {
		if (data == null) {
			data = new ControllerAnalogActionData();
		}

		if (!active) return data;

		_GetAnalogActionData(controller, action, data);

		return data;
	}

	/**
	 * Lookup the handle for an analog (continuos range) action. Best to do this once on startup, and store the handles for all future API calls.
	 *
	 * @param	name	identifier for the action specified in your vdf file (ie, 'Jump', 'Fire', or 'Move')
	 * @return	action analog action handle
	 */
	public function getAnalogActionHandle(name:String):Int {
		if (!active) return -1;
		return _GetAnalogActionHandle(@:privateAccess name.toUtf8());
	}

	/**
	 * Get the origin(s) for an analog action with an action set. Use this to display the appropriate on-screen prompt for the action.
	 * NOTE: Users can change their action origins at any time, and Valve says this is a cheap call and recommends you poll it continuosly
	 * to update your on-screen glyph visuals, rather than calling it rarely and caching the values.
	 *
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	actionSet	handle received from getActionSetHandle()
	 * @param	action	handle received from getAnalogActionHandle()
	 * @param	originsOut	existing array of EControllerActionOrigins you want to fill (optional)
	 * @return the number of origins supplied in originsOut.
	 */

	public function getAnalogActionOrigins(controller:Int, actionSet:Int, action:Int, ?originsOut:Array<EControllerActionOrigin>):Int {
		if (!active) return -1;
		var a:hl.NativeArray<Int> = _GetAnalogActionOrigins(controller, actionSet, action);
		if( a == null ) return 0;

		if (originsOut != null) {
			for (i in 0...a.length) {
				originsOut[i] = cast a[i];
			}
		}

		return a.length;
	}

	/**
	 * Enumerate currently connected controllers
	 *
	 * NOTE: the native steam controller handles are uint64's and too large to easily pass to Haxe,
	 * so the "true" values are left on the C++ side and haxe only deals with 0-based integer indeces
	 * that map back to the "true" values on the C++ side
	 *
	 * @return controller handles
	 */
	static var connectedControllers : hl.NativeArray<Int>;
	public function getConnectedControllers():Array<Int> {
		if (!active) return [];
		connectedControllers = _GetConnectedControllers(connectedControllers);
		return [for(idx in connectedControllers) if( idx >= 0 ) idx];
	}

	/**
	 * Returns the current state of the supplied digital game action
	 *
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	action	handle received from getDigitalActionHandle()
	 * @return
	 */
	public function getDigitalActionData(controller:Int, action:Int):ControllerDigitalActionData {
		if (!active) return new ControllerDigitalActionData(0);
		return new ControllerDigitalActionData(_GetDigitalActionData(controller, action));
	}

	/**
	 * Lookup the handle for a digital (true/false) action. Best to do this once on startup, and store the handles for all future API calls.
	 *
	 * @param	name	identifier for the action specified in your vdf file (ie, 'Jump', 'Fire', or 'Move')
	 * @return	digital action handle
	 */
	public function getDigitalActionHandle(name:String):Int {
		if (!active) return -1;
		return _GetDigitalActionHandle(@:privateAccess name.toUtf8());
	}

	/**
	 * Get the origin(s) for a digital action with an action set. Use this to display the appropriate on-screen prompt for the action.
	 *
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	actionSet	handle received from getActionSetHandle()
	 * @param	action	handle received from getDigitalActionHandle()
	 * @param	originsOut	existing array of EControllerActionOrigins you want to fill (optional)
	 * @return the number of origins supplied in originsOut.
	 */

	public function getDigitalActionOrigins(controller:Int, actionSet:Int, action:Int, ?originsOut:Array<EControllerActionOrigin>):Int {
		if (!active) return 0;
		var a:hl.NativeArray<Int> = _GetDigitalActionOrigins(controller, actionSet, action);
		if( a == null ) return 0;

		//rest of the values are the actual origins
		if (originsOut != null) {
			for (i in 0...a.length) {
				originsOut[i] = cast a[i];
			}
		}

		return a.length;
	}

	/**
	 * Get a local path to art for on-screen glyph for a particular origin
	 * @param	origin
	 * @return
	 */
	public function getGlyphForActionOrigin(origin:EControllerActionOrigin):String {
		var g = _GetGlyphForActionOrigin(origin);
		return g == null ? null : @:privateAccess String.fromUTF8(g);
	}

	/**
	 * Returns a localized string (from Steam's language setting) for the specified origin
	 * @param	origin
	 * @return
	 */
	public function getStringForActionOrigin(origin:EControllerActionOrigin):String {
		var s = _GetStringForActionOrigin(origin);
		return s == null ? null : @:privateAccess String.fromUTF8(s);
	}


	/**
	 * Activates the Steam overlay and shows the input configuration (binding) screen
	 * @return false if overlay is disabled / unavailable, or if the Steam client is not in Big Picture mode
	 */
	public function showBindingPanel(controller:Int):Bool {
		var result:Bool = _ShowBindingPanel(controller);
		return result;
	}


	/**
	 * Activates the Big Picture text input dialog which only supports gamepad input
	 * @param	inputMode	NORMAL or PASSWORD
	 * @param	lineMode	SINGLE_LINE or MULTIPLE_LINES
	 * @param	description	User-facing description of what's being entered, e.g. "Please enter your name"
	 * @param	charMax	Maximum number of characters
	 * @param	existingText	Text to pre-fill the dialog with, if any
	 * @return
	 */
	public function showGamepadTextInput(inputMode:EGamepadTextInputMode, lineMode:EGamepadTextInputLineMode, description:String, charMax:Int = 0xFFFFFF, existingText:String = ""):Bool {
		return _ShowGamepadTextInput(inputMode, lineMode, @:privateAccess description.toUtf8(), charMax, @:privateAccess existingText.toUtf8());
	}

	/**
	 * Returns the text that the player has entered using showGamepadTextInput()
	 * @return
	 */
	public function getEnteredGamepadTextInput():String {
		return @:privateAccess String.fromUTF8(_GetEnteredGamepadTextInput());
	}


	/**
	 * Must be called when ending use of this API
	 */
	public function shutdown() {
		_ShutdownControllers();
		active = false;
	}

	/**
	 * Trigger a haptic pulse in a slightly friendlier way
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	targetPad	which pad you want to pulse
	 * @param	durationMilliSec	duration of the pulse, in milliseconds (1/1000 sec)
	 * @param	strength	value between 0 and 1, general intensity of the pulsing
	 */
	public function hapticPulseRumble(controller:Int, targetPad:ESteamControllerPad, durationMilliSec:Int, strength:Float) {

		if (strength <= 0) return;
		if (strength >  1) strength = 1;

		var durationMicroSec = durationMilliSec * 1000;
		var repeat = 1;

		if (durationMicroSec > MAX_SINGLE_PULSE_TIME)
		{
			repeat = Math.ceil(durationMicroSec / MAX_SINGLE_PULSE_TIME);
			durationMicroSec = MAX_SINGLE_PULSE_TIME;
		}

		var onTime  = Std.int(durationMicroSec * strength);
		var offTime = Std.int(durationMicroSec * (1 - strength));

		if (offTime <= 0) offTime = 1;

		if (repeat > 1)
		{
			triggerRepeatedHapticPulse(controller, targetPad, onTime, offTime, repeat, 0);
		}
		else
		{
			triggerHapticPulse(controller, targetPad, onTime);
		}
	}

	/**
	 * Trigger a single haptic pulse (low-level)
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	targetPad	which pad you want to pulse
	 * @param	durationMicroSec	duration of the pulse, in microseconds (1/1000 ms)
	 */
	public function triggerHapticPulse(controller:Int, targetPad:ESteamControllerPad, durationMicroSec:Int) {
		     if (durationMicroSec < 0) durationMicroSec = 0;
		else if (durationMicroSec > MAX_SINGLE_PULSE_TIME) durationMicroSec = MAX_SINGLE_PULSE_TIME;

		switch(targetPad)
		{
			case LEFT, RIGHT:
				_TriggerHapticPulse(controller, cast targetPad, durationMicroSec);
			case BOTH:
				triggerHapticPulse(controller,  LEFT, durationMicroSec);
				triggerHapticPulse(controller, RIGHT, durationMicroSec);
		}
	}

	/**
	 * Trigger a repeated haptic pulse (low-level)
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	targetPad	which pad you want to pulse
	 * @param	durationMicroSec	duration of the pulse, in microseconds (1/1,000 ms)
	 * @param	offMicroSec	offset between pulses, in microseconds (1/1,000 ms)
	 * @param	repeat	number of pulses
	 * @param	flags	special behavior flags
	 */
	public function triggerRepeatedHapticPulse(controller:Int, targetPad:ESteamControllerPad, durationMicroSec:Int, offMicroSec:Int, repeat:Int, flags:Int) {
		     if (durationMicroSec < 0) durationMicroSec = 0;
		else if (durationMicroSec > MAX_SINGLE_PULSE_TIME) durationMicroSec = MAX_SINGLE_PULSE_TIME;

		switch(targetPad)
		{
			case LEFT, RIGHT:
				_TriggerRepeatedHapticPulse(controller, cast targetPad, durationMicroSec, offMicroSec, repeat, flags);
			case BOTH:
				triggerRepeatedHapticPulse(controller,  LEFT, durationMicroSec, offMicroSec, repeat, flags);
				triggerRepeatedHapticPulse(controller, RIGHT, durationMicroSec, offMicroSec, repeat, flags);
		}
	}

	/**
	 * Trigger a vibration event on supported controllers
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	leftSpeed	how fast the left motor should vibrate (0-65,535)
	 * @param	rightSpeed	how fast the right motor should vibrate (0-65,535)
	 */
	public function triggerVibration(controller:Int, leftSpeed:Int, rightSpeed:Int) {

		if (leftSpeed < 0) leftSpeed = 0;
		if (leftSpeed > 65535) leftSpeed = 65535;
		if (rightSpeed < 0) rightSpeed = 0;
		if (rightSpeed > 65535) rightSpeed = 65535;
		_TriggerVibration(controller, leftSpeed, rightSpeed);

	}

	/**
	 * Set the controller LED color on supported controllers.
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	rgb	an RGB color in 0xRRGGBB format
	 * @param	flags	bit-masked flags combined from values defined in ESteamControllerLEDFlags
	 */
	public function setLEDColor(controller:Int, rgb:Int, flags:Int=ESteamControllerLEDFlags.SET_COLOR) {

		var r = (rgb >> 16) & 0xFF;
		var g = (rgb >> 8) & 0xFF;
		var b = rgb & 0xFF;
		_SetLEDColor(controller, r, g, b, flags);

	}

	public function resetLEDColor(controller:Int) {
		_SetLEDColor(controller, 0, 0, 0, ESteamControllerLEDFlags.RESTORE_USER_DEFAULT);
	}


	public function getGamepadIndexForController(controller:Int):Int {
		//TODO
		return -1;
	}


	public function getControllerForGamepadIndex(index:Int):Int {
		//TODO
		return -1;
	}

	/**
	 * Returns the current state of the supplied analog game action
	 *
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	data	existing ControllerMotionData structure you want to fill (optional)
	 * @return	data structure containing motion data values
	 */
	public function getMotionData(controller:Int, ?data:ControllerMotionData):ControllerMotionData{
		if (data == null) {
			data = new ControllerMotionData();
		}

		if (!active) return data;

		_GetMotionData(controller, data);


		return data;
	}

	/**
	 * Attempt to display origins of given action in the controller HUD, for the currently active action set
	 * Returns false is overlay is disabled / unavailable, or the user is not in Big Picture mode
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	digitalActionHandle	handle received from getDigitalActionHandle()
	 * @param	scale	scale multiplier to apply to the on-screen display (1.0 is 1:1 size)
	 * @param	xPosition	position of the on-screen display (0.5 is the center)
	 * @param	yPosition	position of the on-screen display (0.5 is the center)
	 */
	public function showDigitalActionOrigins(controller:Int, digitalActionHandle:Int, scale:Float, xPosition:Float, yPosition:Float) {

		_ShowDigitalActionOrigins(controller, digitalActionHandle, scale, xPosition, yPosition);

	}

	/**
	 * Attempt to display origins of given action in the controller HUD, for the currently active action set
	 * Returns false is overlay is disabled / unavailable, or the user is not in Big Picture mode
	 * @param	controller	handle received from getConnectedControllers()
	 * @param	analogActionHandle	handle received from getDigitalActionHandle()
	 * @param	scale	scale multiplier to apply to the on-screen display (1.0 is 1:1 size)
	 * @param	xPosition	position of the on-screen display (0.5 is the center)
	 * @param	yPosition	position of the on-screen display (0.5 is the center)
	 */
	public function showAnalogActionOrigins(controller:Int, analogActionHandle:Int, scale:Float, xPosition:Float, yPosition:Float) {

		_ShowAnalogActionOrigins(controller, analogActionHandle, scale, xPosition, yPosition);

	}

	/*************PRIVATE***************/

	private var customTrace:String->Void;

	@:hlNative("steam","init_controllers") private static function _InitControllers() : Bool { return false; }
	@:hlNative("steam","shutdown_controllers") private static function _ShutdownControllers() : Bool { return false; }
	@:hlNative("steam","get_connected_controllers") private static function _GetConnectedControllers( arr : hl.NativeArray<Int> ) : hl.NativeArray<Int> { return null; }
	@:hlNative("steam","get_digital_action_origins") private static function _GetDigitalActionOrigins( controller : Int, action : Int, digitalAction : Int ) : hl.NativeArray<Int> { return null; }
	@:hlNative("steam","get_entered_gamepad_text_input") private static function _GetEnteredGamepadTextInput() : hl.Bytes { return null; }
	@:hlNative("steam","get_analog_action_origins") private static function _GetAnalogActionOrigins( controller : Int, action : Int, analogAction : Int ) : hl.NativeArray<Int> { return null; }
	@:hlNative("steam","show_binding_panel") private static function _ShowBindingPanel( controller : Int ) : Bool { return false; }
	@:hlNative("steam","get_string_for_action_origin") private static function _GetStringForActionOrigin( origin : Int ) : hl.Bytes { return null; }
	@:hlNative("steam","get_glyph_for_action_origin") private static function _GetGlyphForActionOrigin( origin : Int ) : hl.Bytes { return null; }
	@:hlNative("steam","get_controller_max_count") private static function _GetControllerMaxCount() : Int { return 0; }
	@:hlNative("steam","get_controller_max_analog_actions") private static function _GetControllerMaxAnalogActions() : Int { return 0; }
	@:hlNative("steam","get_controller_max_digital_actions") private static function _GetControllerMaxDigitalActions() : Int { return 0; }
	@:hlNative("steam","get_controller_max_origins") private static function _GetControllerMaxOrigins() : Int { return 0; }
	@:hlNative("steam","get_controller_max_analog_action_data") private static function _GetControllerMaxAnalogActionData() : Float { return 0.; }
	@:hlNative("steam","get_controller_min_analog_action_data") private static function _GetControllerMinAnalogActionData() : Float { return 0.; }
	@:hlNative("steam","activate_action_set") private static function _ActivateActionSet( controller : Int, action : Int ): Void{};
	@:hlNative("steam","get_current_action_set") private static function _GetCurrentActionSet( controller : Int ) : Int { return 0; }
	@:hlNative("steam","get_action_set_handle") private static function _GetActionSetHandle( name : hl.Bytes ) : Int { return 0; }
	@:hlNative("steam","get_analog_action_data") private static function _GetAnalogActionData( controller : Int, action : Int, data : ControllerAnalogActionData ): Void{};
	@:hlNative("steam","get_analog_action_handle") private static function _GetAnalogActionHandle( name : hl.Bytes ) : Int { return 0; }
	@:hlNative("steam","get_digital_action_data") private static function _GetDigitalActionData( controller : Int, action : Int ) : Int { return 0; }
	@:hlNative("steam","get_digital_action_handle") private static function _GetDigitalActionHandle( name : hl.Bytes ) : Int { return 0; }
	@:hlNative("steam","show_gamepad_text_input") private static function _ShowGamepadTextInput( inputMode : Int, lineMode : Int, description : hl.Bytes, charMax : Int, existingText : hl.Bytes ) : Bool { return false; }
	@:hlNative("steam","trigger_haptic_pulse") private static function _TriggerHapticPulse( controller : Int, targetPad : Int, duration : Int ): Void{};
	@:hlNative("steam","trigger_repeated_haptic_pulse") private static function _TriggerRepeatedHapticPulse( controller : Int, targetPad : Int, duration : Int, off : Int, repeat : Int, flags : Int ): Void{};
	@:hlNative("steam","trigger_vibration") private static function _TriggerVibration( controller : Int, leftSpeed : Int, rightSpeed : Int ): Void{};
	@:hlNative("steam","set_led_color") private static function _SetLEDColor( controller : Int, r : Int, g : Int, b : Int, flags : Int ): Void{};
	@:hlNative("steam","get_motion_data") private static function _GetMotionData( controller : Int, data : ControllerMotionData ): Void{};
	@:hlNative("steam","show_digital_action_origins") private static function _ShowDigitalActionOrigins( controller : Int, digitalAction : Int, scale : Float, xPos : Float, yPos : Float ) : Bool { return false; }
	@:hlNative("steam","show_analog_action_origins") private static function _ShowAnalogActionOrigins( controller : Int, analogAction : Int, scale : Float, xPos : Float, yPos : Float ) : Bool { return false; }


	private function new(CustomTrace:String->Void)
	{
		customTrace = CustomTrace;
		init();
	}

	private function init()
	{
		if (active) return;

		// if we get this far, the dlls loaded ok and we need Steam controllers to init.
		// otherwise, we're trying to run the Steam version without the Steam client
		active = _InitControllers();
	}

	private var max_controllers:Int = -1;
	private function get_MAX_CONTROLLERS():Int
	{
		if(max_controllers == -1)
			max_controllers = _GetControllerMaxCount();
		return max_controllers;
	}

	private var max_analog_actions = -1;
	private function get_MAX_ANALOG_ACTIONS():Int
	{
		if(max_analog_actions == -1)
			max_analog_actions = _GetControllerMaxAnalogActions();
		return max_analog_actions;
	}

	private var max_digital_actions = -1;
	private function get_MAX_DIGITAL_ACTIONS():Int
	{
		if (max_digital_actions == -1)
			max_digital_actions = _GetControllerMaxDigitalActions();
		return max_digital_actions;
	}

	private var max_origins = -1;
	private function get_MAX_ORIGINS():Int
	{
		if(max_origins == -1)
			max_origins = _GetControllerMaxOrigins();
		return max_origins;
	}

	private var max_analog_value = Math.NaN;
	private function get_MAX_ANALOG_VALUE():Float
	{
		if(Math.isNaN(max_analog_value))
			max_analog_value = _GetControllerMaxAnalogActionData();
		return max_analog_value;
	}

	private var min_analog_value = Math.NaN;
	private function get_MIN_ANALOG_VALUE():Float
	{
		if(Math.isNaN(min_analog_value))
			min_analog_value = _GetControllerMinAnalogActionData();
		return min_analog_value;
	}
}

abstract ControllerDigitalActionData(Int) from Int to Int{

	public function new(i:Int) {
		this = i;
	}

	public var bState(get, never):Bool;
	private function get_bState():Bool { return this & 0x1 == 0x1; }

	public var bActive(get, never):Bool;
	private function get_bActive():Bool { return this & 0x10 == 0x10; }
}

class ControllerAnalogActionData
{
	public var bActive:Bool = false;
	public var eMode:EControllerSourceMode = NONE;
	public var x:Float = 0.0;
	public var y:Float = 0.0;

	public function new(){}
}

class ControllerMotionData
{
	// Sensor-fused absolute rotation; will drift in heading
	public var rotQuatX:Float = 0.0;
	public var rotQuatY:Float = 0.0;
	public var rotQuatZ:Float = 0.0;
	public var rotQuatW:Float = 0.0;

	// Positional acceleration
	public var posAccelX:Float = 0.0;
	public var posAccelY:Float = 0.0;
	public var posAccelZ:Float = 0.0;

	// Angular velocity
	public var rotVelX:Float = 0.0;
	public var rotVelY:Float = 0.0;
	public var rotVelZ:Float = 0.0;

	public function new(){}

	public function toString():String{
		return "ControllerMotionData{rotQuad:(" + rotQuatX + "," + rotQuatY + "," + rotQuatZ + "," + rotQuatW + "), " +
									"posAccel:(" + posAccelX + "," + posAccelY + "," + posAccelZ + "), " +
									"rotVel:(" + rotVelX + "," + rotVelY + "," + rotVelZ + ")}";
	}
}

class ESteamControllerLEDFlags {

	public static inline var SET_COLOR = 0x01;
	public static inline var RESTORE_USER_DEFAULT = 0x10;

}

enum abstract EControllerActionOrigin(Int) to Int {

	public static var fromStringMap(default, null):Map<String, EControllerActionOrigin>
		= MacroHelper.buildMap("steam.EControllerActionOrigin");

	public static var toStringMap(default, null):Map<EControllerActionOrigin, String>
		= MacroHelper.buildMap("steam.EControllerActionOrigin", true);

	public var NONE = 0;

	//Valve Steam Controller:
	public var A = 1;
	public var B = 2;
	public var X = 3;
	public var Y = 4;
	public var LEFTBUMPER= 5;
	public var RIGHTBUMPER= 6;
	public var LEFTGRIP = 7;
	public var RIGHTGRIP = 8;
	public var START = 9;
	public var BACK = 10;
	public var LEFTPAD_TOUCH = 11;
	public var LEFTPAD_SWIPE = 12;
	public var LEFTPAD_CLICK = 13;
	public var LEFTPAD_DPADNORTH = 14;
	public var LEFTPAD_DPADSOUTH = 15;
	public var LEFTPAD_DPADWEST = 16;
	public var LEFTPAD_DPADEAST = 17;
	public var RIGHTPAD_TOUCH = 18;
	public var RIGHTPAD_SWIPE = 19;
	public var RIGHTPAD_CLICK = 20;
	public var RIGHTPAD_DPADNORTH = 21;
	public var RIGHTPAD_DPADSOUTH = 22;
	public var RIGHTPAD_DPADWEST = 23;
	public var RIGHTPAD_DPADEAST = 24;
	public var LEFTTRIGGER_PULL = 25;
	public var LEFTTRIGGER_CLICK = 26;
	public var RIGHTTRIGGER_PULL = 27;
	public var RIGHTTRIGGER_CLICK = 28;
	public var LEFTSTICK_MOVE = 29;
	public var LEFTSTICK_CLICK = 30;
	public var LEFTSTICK_DPADNORTH = 31;
	public var LEFTSTICK_DPADSOUTH = 32;
	public var LEFTSTICK_DPADWEST = 33;
	public var LEFTSTICK_DPADEAST = 34;
	public var GYRO_MOVE = 35;
	public var GYRO_PITCH = 36;
	public var GYRO_YAW = 37;
	public var GYRO_ROLL = 38;

	//Sony PlayStation DualShock 4:
	public var PS4_X = 39;
	public var PS4_CIRCLE = 40;
	public var PS4_TRIANGLE = 41;
	public var PS4_SQUARE = 42;
	public var PS4_LEFTBUMPER = 43;
	public var PS4_RIGHTBUMPER = 44;
	public var PS4_OPTIONS = 45;
	public var PS4_SHARE = 46;
	public var PS4_LEFTPAD_TOUCH = 47;
	public var PS4_LEFTPAD_SWIPE = 48;
	public var PS4_LEFTPAD_CLICK = 49;
	public var PS4_LEFTPAD_DPADNORTH = 50;
	public var PS4_LEFTPAD_DPADSOUTH = 51;
	public var PS4_LEFTPAD_DPADWEST = 52;
	public var PS4_LEFTPAD_DPADEAST = 53;
	public var PS4_RIGHTPAD_TOUCH = 54;
	public var PS4_RIGHTPAD_SWIPE = 55;
	public var PS4_RIGHTPAD_CLICK = 56;
	public var PS4_RIGHTPAD_DPADNORTH = 57;
	public var PS4_RIGHTPAD_DPADSOUTH = 58;
	public var PS4_RIGHTPAD_DPADWEST = 59;
	public var PS4_RIGHTPAD_DPADEAST = 60;
	public var PS4_CENTERPAD_TOUCH = 61;
	public var PS4_CENTERPAD_SWIPE = 62;
	public var PS4_CENTERPAD_CLICK = 63;
	public var PS4_CENTERPAD_DPADNORTH = 64;
	public var PS4_CENTERPAD_DPADSOUTH = 65;
	public var PS4_CENTERPAD_DPADWEST = 66;
	public var PS4_CENTERPAD_DPADEAST = 67;
	public var PS4_LEFTTRIGGER_PULL = 68;
	public var PS4_LEFTTRIGGER_CLICK = 69;
	public var PS4_RIGHTTRIGGER_PULL = 70;
	public var PS4_RIGHTTRIGGER_CLICK = 71;
	public var PS4_LEFTSTICK_MOVE = 72;
	public var PS4_LEFTSTICK_CLICK = 73;
	public var PS4_LEFTSTICK_DPADNORTH = 74;
	public var PS4_LEFTSTICK_DPADSOUTH = 75;
	public var PS4_LEFTSTICK_DPADWEST = 76;
	public var PS4_LEFTSTICK_DPADEAST = 77;
	public var PS4_RIGHTSTICK_MOVE = 78;
	public var PS4_RIGHTSTICK_CLICK = 79;
	public var PS4_RIGHTSTICK_DPADNORTH = 80;
	public var PS4_RIGHTSTICK_DPADSOUTH = 81;
	public var PS4_RIGHTSTICK_DPADWEST = 82;
	public var PS4_RIGHTSTICK_DPADEAST = 83;
	public var PS4_DPAD_NORTH = 84;
	public var PS4_DPAD_SOUTH = 85;
	public var PS4_DPAD_WEST = 86;
	public var PS4_DPAD_EAST = 87;
	public var PS4_GYRO_MOVE = 88;
	public var PS4_GYRO_PITCH = 89;
	public var PS4_GYRO_YAW = 90;
	public var PS4_GYRO_ROLL = 91;

	//Microsoft XBox One:
	public var XBOXONE_A = 92;
	public var XBOXONE_B = 93;
	public var XBOXONE_X = 94;
	public var XBOXONE_Y = 95;
	public var XBOXONE_LEFTBUMPER = 96;
	public var XBOXONE_RIGHTBUMPER = 97;
	public var XBOXONE_MENU = 98;
	public var XBOXONE_VIEW = 99;
	public var XBOXONE_LEFTTRIGGER_PULL = 100;
	public var XBOXONE_LEFTTRIGGER_CLICK = 101;
	public var XBOXONE_RIGHTTRIGGER_PULL = 102;
	public var XBOXONE_RIGHTTRIGGER_CLICK = 103;
	public var XBOXONE_LEFTSTICK_MOVE = 104;
	public var XBOXONE_LEFTSTICK_CLICK = 105;
	public var XBOXONE_LEFTSTICK_DPADNORTH = 106;
	public var XBOXONE_LEFTSTICK_DPADSOUTH = 107;
	public var XBOXONE_LEFTSTICK_DPADWEST = 108;
	public var XBOXONE_LEFTSTICK_DPADEAST = 109;
	public var XBOXONE_RIGHTSTICK_MOVE = 110;
	public var XBOXONE_RIGHTSTICK_CLICK = 111;
	public var XBOXONE_RIGHTSTICK_DPADNORTH = 112;
	public var XBOXONE_RIGHTSTICK_DPADSOUTH = 113;
	public var XBOXONE_RIGHTSTICK_DPADWEST = 114;
	public var XBOXONE_RIGHTSTICK_DPADEAST = 115;
	public var XBOXONE_DPAD_NORTH = 116;
	public var XBOXONE_DPAD_SOUTH = 117;
	public var XBOXONE_DPAD_WEST = 118;
	public var XBOXONE_DPAD_EAST = 119;

	//Microsoft XBox 360:
	public var XBOX360_A = 120;
	public var XBOX360_B = 121;
	public var XBOX360_X = 122;
	public var XBOX360_Y = 123;
	public var XBOX360_LEFTBUMPER = 124;
	public var XBOX360_RIGHTBUMPER = 125;
	public var XBOX360_START = 126;
	public var XBOX360_BACK = 127;
	public var XBOX360_LEFTTRIGGER_PULL = 128;
	public var XBOX360_LEFTTRIGGER_CLICK = 129;
	public var XBOX360_RIGHTTRIGGER_PULL = 130;
	public var XBOX360_RIGHTTRIGGER_CLICK = 131;
	public var XBOX360_LEFTSTICK_MOVE = 132;
	public var XBOX360_LEFTSTICK_CLICK = 133;
	public var XBOX360_LEFTSTICK_DPADNORTH = 134;
	public var XBOX360_LEFTSTICK_DPADSOUTH = 135;
	public var XBOX360_LEFTSTICK_DPADWEST = 136;
	public var XBOX360_LEFTSTICK_DPADEAST = 137;
	public var XBOX360_RIGHTSTICK_MOVE = 138;
	public var XBOX360_RIGHTSTICK_CLICK = 139;
	public var XBOX360_RIGHTSTICK_DPADNORTH = 140;
	public var XBOX360_RIGHTSTICK_DPADSOUTH = 141;
	public var XBOX360_RIGHTSTICK_DPADWEST = 142;
	public var XBOX360_RIGHTSTICK_DPADEAST = 143;
	public var XBOX360_DPAD_NORTH = 144;
	public var XBOX360_DPAD_SOUTH = 145;
	public var XBOX360_DPAD_WEST = 146;
	public var XBOX360_DPAD_EAST = 147;

	public var COUNT = 148;

	public var UNKNOWN = -1;

	@:from private static function fromString (s:String):EControllerActionOrigin {

		var i = Std.parseInt(s);

		if (i == null)
		{
			//if it's not a numeric value, try to interpret it from its name
			s = s.toUpperCase();
			return fromStringMap.exists(s) ? fromStringMap.get(s) : UNKNOWN;
		}

		return cast Std.int(i);

	}

	@:to public inline function toString():String {

		if (toStringMap.exists(cast this))
		{
			return toStringMap.get(cast this);
		}

		return "unknown";
	}

	/**
	 * Returns the string name for this glyph so you can load a corresponding image from your assets.
	 * @param	value the integer value of a controller action origin from the steam API
	 * @return	a unique string identifier for this glyph, OR "unknown" if the glyph is not known (see Controller.getGlyphForActionOrigin())
	 */
	public static function getGlyph(value:EControllerActionOrigin):String {
		return switch(value)
		{
			case NONE:               "none";

			//Valve Steam Controller:
			case A:                  "button_a";
			case B:                  "button_b";
			case X:                  "button_x";
			case Y:                  "button_y";
			case LEFTBUMPER:         "shoulder_l";
			case RIGHTBUMPER:        "shoulder_r";
			case LEFTGRIP:           "grip_l";
			case RIGHTGRIP:          "grip_r";
			case START:              "button_start";
			case BACK:               "button_select";
			case LEFTPAD_TOUCH:      "pad_l_touch";
			case LEFTPAD_SWIPE:      "pad_l_swipe";
			case LEFTPAD_CLICK:      "pad_l_click";
			case LEFTPAD_DPADNORTH:  "pad_l_dpad_n";
			case LEFTPAD_DPADSOUTH:  "pad_l_dpad_s";
			case LEFTPAD_DPADWEST:   "pad_l_dpad_w";
			case LEFTPAD_DPADEAST:   "pad_l_dpad_e";
			case RIGHTPAD_TOUCH:     "pad_r_touch";
			case RIGHTPAD_SWIPE:     "pad_r_swipe";
			case RIGHTPAD_CLICK:     "pad_r_click";
			case RIGHTPAD_DPADNORTH: "pad_r_dpad_n";
			case RIGHTPAD_DPADSOUTH: "pad_r_dpad_s";
			case RIGHTPAD_DPADWEST:  "pad_r_dpad_w";
			case RIGHTPAD_DPADEAST:  "pad_r_dpad_e";
			case LEFTTRIGGER_PULL:   "trigger_l_pull";
			case LEFTTRIGGER_CLICK:  "trigger_l_click";
			case RIGHTTRIGGER_PULL:  "trigger_r_pull";
			case RIGHTTRIGGER_CLICK: "trigger_r_click";
			case LEFTSTICK_MOVE:     "stick_move";
			case LEFTSTICK_CLICK:    "stick_click";
			case LEFTSTICK_DPADNORTH:"stick_dpad_n";
			case LEFTSTICK_DPADSOUTH:"stick_dpad_s";
			case LEFTSTICK_DPADWEST: "stick_dpad_w";
			case LEFTSTICK_DPADEAST: "stick_dpad_e";
			case GYRO_MOVE:          "gyro";
			case GYRO_PITCH:         "gyro_pitch";
			case GYRO_YAW:           "gyro_yaw";
			case GYRO_ROLL:          "gyro_roll";

			//Sony PlayStation DualShock 4:
			case PS4_X:                    "ps4_button_x";
			case PS4_CIRCLE:               "ps4_button_circle";
			case PS4_TRIANGLE:             "ps4_button_triangle";
			case PS4_SQUARE:               "ps4_button_square";
			case PS4_LEFTBUMPER:           "ps4_shoulder_l";
			case PS4_RIGHTBUMPER:          "ps4_shouldr_r";
			case PS4_OPTIONS:              "ps4_button_options";
			case PS4_SHARE:                "ps4_button_share";
			case PS4_LEFTPAD_TOUCH :       "ps4_pad_l_touch";
			case PS4_LEFTPAD_SWIPE:        "ps4_pad_l_swipe";
			case PS4_LEFTPAD_CLICK:        "ps4_pad_l_click";
			case PS4_LEFTPAD_DPADNORTH:    "ps4_pad_l_dpad_n";
			case PS4_LEFTPAD_DPADSOUTH:    "ps4_pad_l_dpad_s";
			case PS4_LEFTPAD_DPADWEST:     "ps4_pad_l_dpad_w";
			case PS4_LEFTPAD_DPADEAST:     "ps4_pad_l_dpad_e";
			case PS4_RIGHTPAD_TOUCH:       "ps4_pard_r_touch";
			case PS4_RIGHTPAD_SWIPE:       "ps4_pad_r_swipe";
			case PS4_RIGHTPAD_CLICK:       "ps4_pad_r_click";
			case PS4_RIGHTPAD_DPADNORTH:   "ps4_pad_r_dpad_n";
			case PS4_RIGHTPAD_DPADSOUTH:   "ps4_pad_r_dpad_s";
			case PS4_RIGHTPAD_DPADWEST:    "ps4_pad_r_dpad_w";
			case PS4_RIGHTPAD_DPADEAST:    "ps4_pad_r_dpad_e";
			case PS4_CENTERPAD_TOUCH :     "ps4_pad_center_touch";
			case PS4_CENTERPAD_SWIPE:      "ps4_pad_center_swipe";
			case PS4_CENTERPAD_CLICK:      "ps4_pad_center_click";
			case PS4_CENTERPAD_DPADNORTH:  "ps4_pad_center_dpad_n";
			case PS4_CENTERPAD_DPADSOUTH:  "ps4_pad_center_dpad_s";
			case PS4_CENTERPAD_DPADWEST:   "ps4_pad_center_dpad_w";
			case PS4_CENTERPAD_DPADEAST:   "ps4_pad_center_dpad_e";
			case PS4_LEFTTRIGGER_PULL:     "ps4_trigger_l_pull";
			case PS4_LEFTTRIGGER_CLICK:    "ps4_trigger_l_click";
			case PS4_RIGHTTRIGGER_PULL:    "ps4_trigger_r_pull";
			case PS4_RIGHTTRIGGER_CLICK:   "ps4_trigger_r_click";
			case PS4_LEFTSTICK_MOVE:       "ps4_stick_l_move";
			case PS4_LEFTSTICK_CLICK:      "ps4_stick_l_click";
			case PS4_LEFTSTICK_DPADNORTH:  "ps4_stick_l_dpad_n";
			case PS4_LEFTSTICK_DPADSOUTH:  "ps4_stick_l_dpad_s";
			case PS4_LEFTSTICK_DPADWEST:   "ps4_stick_l_dpad_w";
			case PS4_LEFTSTICK_DPADEAST:   "ps4_stick_l_dpad_e";
			case PS4_RIGHTSTICK_MOVE:      "ps4_stick_r_move";
			case PS4_RIGHTSTICK_CLICK:     "ps4_stick_r_click";
			case PS4_RIGHTSTICK_DPADNORTH: "ps4_stick_r_dpad_n";
			case PS4_RIGHTSTICK_DPADSOUTH: "ps4_stick_r_dpad_s";
			case PS4_RIGHTSTICK_DPADWEST:  "ps4_stick_r_dpad_w";
			case PS4_RIGHTSTICK_DPADEAST:  "ps4_stick_r_dpad_e";
			case PS4_DPAD_NORTH:           "ps4_button_dpad_n";
			case PS4_DPAD_SOUTH:           "ps4_button_dpad_s";
			case PS4_DPAD_WEST:            "ps4_button_dpad_w";
			case PS4_DPAD_EAST:            "ps4_button_dpad_e";
			case PS4_GYRO_MOVE:            "ps4_gyro";
			case PS4_GYRO_PITCH:           "ps4_gyro_pitch";
			case PS4_GYRO_YAW:             "ps4_gyro_yaw";
			case PS4_GYRO_ROLL:            "ps4_gyro_roll";

			//Microsoft XBox One:
			case XBOXONE_A:                    "xboxone_button_a";
			case XBOXONE_B:                    "xboxone_button_b";
			case XBOXONE_X:                    "xboxone_button_x";
			case XBOXONE_Y:                    "xboxone_button_y";
			case XBOXONE_LEFTBUMPER:           "xboxone_shoulder_l";
			case XBOXONE_RIGHTBUMPER:          "xboxone_shouldr_r";
			case XBOXONE_MENU:                 "xboxone_button_menu";
			case XBOXONE_VIEW:                 "xboxone_button_view";
			case XBOXONE_LEFTTRIGGER_PULL:     "xboxone_trigger_l_pull";
			case XBOXONE_LEFTTRIGGER_CLICK:    "xboxone_trigger_l_click";
			case XBOXONE_RIGHTTRIGGER_PULL:    "xboxone_trigger_r_pull";
			case XBOXONE_RIGHTTRIGGER_CLICK:   "xboxone_trigger_r_click";
			case XBOXONE_LEFTSTICK_MOVE:       "xboxone_stick_l_move";
			case XBOXONE_LEFTSTICK_CLICK:      "xboxone_stick_l_click";
			case XBOXONE_LEFTSTICK_DPADNORTH:  "xboxone_stick_l_dpad_n";
			case XBOXONE_LEFTSTICK_DPADSOUTH:  "xboxone_stick_l_dpad_s";
			case XBOXONE_LEFTSTICK_DPADWEST:   "xboxone_stick_l_dpad_w";
			case XBOXONE_LEFTSTICK_DPADEAST:   "xboxone_stick_l_dpad_e";
			case XBOXONE_RIGHTSTICK_MOVE:      "xboxone_stick_r_move";
			case XBOXONE_RIGHTSTICK_CLICK:     "xboxone_stick_r_click";
			case XBOXONE_RIGHTSTICK_DPADNORTH: "xboxone_stick_r_dpad_n";
			case XBOXONE_RIGHTSTICK_DPADSOUTH: "xboxone_stick_r_dpad_s";
			case XBOXONE_RIGHTSTICK_DPADWEST:  "xboxone_stick_r_dpad_w";
			case XBOXONE_RIGHTSTICK_DPADEAST:  "xboxone_stick_r_dpad_e";
			case XBOXONE_DPAD_NORTH:           "xboxone_button_dpad_n";
			case XBOXONE_DPAD_SOUTH:           "xboxone_button_dpad_s";
			case XBOXONE_DPAD_WEST:            "xboxone_button_dpad_w";
			case XBOXONE_DPAD_EAST:            "xboxone_button_dpad_e";

			//Microsoft XBox 360:
			case XBOX360_A:                    "xbox360_button_a";
			case XBOX360_B:                    "xbox360_button_b";
			case XBOX360_X:                    "xbox360_button_x";
			case XBOX360_Y:                    "xbox360_button_y";
			case XBOX360_LEFTBUMPER:           "xbox360_shoulder_l";
			case XBOX360_RIGHTBUMPER:          "xbox360_shouldr_r";
			case XBOX360_START:                "xbox360_button_start";
			case XBOX360_BACK:                 "xbox360_button_back";
			case XBOX360_LEFTTRIGGER_PULL:     "xbox360_trigger_l_pull";
			case XBOX360_LEFTTRIGGER_CLICK:    "xbox360_trigger_l_click";
			case XBOX360_RIGHTTRIGGER_PULL:    "xbox360_trigger_r_pull";
			case XBOX360_RIGHTTRIGGER_CLICK:   "xbox360_trigger_r_click";
			case XBOX360_LEFTSTICK_MOVE:       "xbox360_stick_l_move";
			case XBOX360_LEFTSTICK_CLICK:      "xbox360_stick_l_click";
			case XBOX360_LEFTSTICK_DPADNORTH:  "xbox360_stick_l_dpad_n";
			case XBOX360_LEFTSTICK_DPADSOUTH:  "xbox360_stick_l_dpad_s";
			case XBOX360_LEFTSTICK_DPADWEST:   "xbox360_stick_l_dpad_w";
			case XBOX360_LEFTSTICK_DPADEAST:   "xbox360_stick_l_dpad_e";
			case XBOX360_RIGHTSTICK_MOVE:      "xbox360_stick_r_move";
			case XBOX360_RIGHTSTICK_CLICK:     "xbox360_stick_r_click";
			case XBOX360_RIGHTSTICK_DPADNORTH: "xbox360_stick_r_dpad_n";
			case XBOX360_RIGHTSTICK_DPADSOUTH: "xbox360_stick_r_dpad_s";
			case XBOX360_RIGHTSTICK_DPADWEST:  "xbox360_stick_r_dpad_w";
			case XBOX360_RIGHTSTICK_DPADEAST:  "xbox360_stick_r_dpad_e";
			case XBOX360_DPAD_NORTH:           "xbox360_button_dpad_n";
			case XBOX360_DPAD_SOUTH:           "xbox360_button_dpad_s";
			case XBOX360_DPAD_WEST:            "xbox360_button_dpad_w";
			case XBOX360_DPAD_EAST:            "xbox360_button_dpad_e";

			default:                           "unknown";
		}
	}

}

enum abstract ESteamControllerPad(Int) to Int {
	public var LEFT = 0;
	public var RIGHT = 1;
	public var BOTH = 2;
}

enum abstract EControllerSource(Int) to Int {
	public var NONE = 0;
	public var LEFTTRACKPAD = 1;
	public var RIGHTTRACKPAD = 2;
	public var JOYSTICK = 3;
	public var ABXY = 4;
	public var SWITCH = 5;
	public var LEFTTRIGGER = 6;
	public var RIGHTTRIGGER = 7;
	public var GYRO = 8;
	public var COUNT = 9;
}

enum abstract EControllerSourceMode(Int) to Int {
	public var NONE = 0;
	public var DPAD = 1;
	public var BUTTONS = 2;
	public var FOURBUTTONS = 3;
	public var ABSOLUTEMOUSE = 4;
	public var RELATIVEMOUSE = 5;
	public var JOYSTICKMOVE = 6;
	public var JOYSTICKCAMERA = 7;
	public var SCROLLWHEEL = 8;
	public var TRIGGER = 9;
	public var TOUCHMENU = 10;
	public var MOUSEJOYSTICK = 11;
	public var MOUSEREGION = 12;
}

enum abstract EGamepadTextInputLineMode(Int) to Int {
	public var SINGLE_LINE = 0;
	public var MULTIPLE_LINES = 1;
}

enum abstract EGamepadTextInputMode(Int) to Int {
	public var NORMAL = 0;
	public var PASSWORD = 1;
}

