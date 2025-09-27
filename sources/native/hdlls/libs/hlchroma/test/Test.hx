package test;
import Math.*;

class Test extends hxd.App {

	static function main(){
		new Test();
	}

	override function init(){
		chroma.Api.init();
		keyboardEffect = new chroma.Api.KeyboardCustomKeyEffect();
		mouseEffect = new chroma.Api.MouseCustomEffect();
		mousepadEffect = new chroma.Api.MousepadCustomEffect();
		headsetEffect = new chroma.Api.HeadsetCustomEffect();
		
		chroma.Api.setKeyboardKeysEffect(keyboardEffect);
		//chroma.Api.setPattern(patternId);
	}

	override function update(dt : Float){
		for( i in 0 ... 50 ){
			var col = chroma.Api.Color.fromRGB(floor(random() * 255), floor(random() * 255), floor(random() * 255));
			keyboardEffect.SetBaseColor(floor(random() * chroma.Api.Keyboard.maxRowCount), floor(random() * chroma.Api.Keyboard.maxColCount), col);	
			if( i == 1 ){
				headsetEffect.SetColor(floor(random() * chroma.Api.Headset.maxLedsCount), col);
			}
			
			if( i < 20 ){
				mouseEffect.SetColor(floor(random() * chroma.Api.Mouse.maxRowCount), floor(random() * chroma.Api.Mouse.maxColCount), col);
				mousepadEffect.SetColor(floor(random() * chroma.Api.Mousepad.maxLedsCount), col);
			}

		}
		chroma.Api.setKeyboardKeysEffect(keyboardEffect);
		chroma.Api.setMouseEffect(mouseEffect);
		chroma.Api.setMousepadEffect(mousepadEffect);
		chroma.Api.setHeadsetEffect(headsetEffect);
	}

	public var keyboardEffect : chroma.Api.KeyboardCustomKeyEffect;
	public var mouseEffect : chroma.Api.MouseCustomEffect;
	public var mousepadEffect : chroma.Api.MousepadCustomEffect;
	public var headsetEffect : chroma.Api.HeadsetCustomEffect;
}