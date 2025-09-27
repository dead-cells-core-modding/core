package test;


class Test extends hxd.App {

	var accum : Float;
	static function main(){
		new Test();
	}

	override function init(){
		discord.Api.init("11111111111111", "22222222");
		accum = 15;
	}

	override function update(dt : Float){
		accum += (dt / 60);
		if( accum > 15 ){
			discord.Api.updateState("State Test", false);
			discord.Api.updateDetails("Details");
			accum = 0;
		} 
	}
}