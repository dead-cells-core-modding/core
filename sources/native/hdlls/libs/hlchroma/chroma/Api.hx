package chroma;

class Keyboard{
    public static inline var maxRowCount = 6;
    public static inline var maxColCount = 22;
}

private class Utilities{
    public static inline function swapColor(hlcolor : Int) : Int {
        var r = hlcolor >> 16;
        var g = hlcolor & 0x00FF00;
        var b = hlcolor & 0x0000FF;
        return r | g | (b << 16);
    }
}

abstract KeyboardCustomEffect(hl.Bytes){
    public function new(){
        this = new hl.Bytes(Keyboard.maxRowCount * Keyboard.maxColCount * 4);
    }

    public function clearWithColor(color : Int){
        for( i in 0 ... Keyboard.maxColCount * Keyboard.maxRowCount){
            this.setI32(i * 4, Utilities.swapColor(color));
        }
    }

    public function setColor(row : Int, column : Int, color : Int){
        var index = row * Keyboard.maxColCount + column;
        if(index < Keyboard.maxColCount * Keyboard.maxRowCount){
            this.setI32(index * 4, Utilities.swapColor(color));
        }
    }
}

abstract KeyboardCustomKeyEffect(hl.Bytes){
    public function new(){
        this = new hl.Bytes(Keyboard.maxRowCount * Keyboard.maxColCount * 4 * 2);
    }

    public function get(){
        return this;
    }
    public function copyBaseColor(effect : hl.Bytes){
        for( i in 0 ... Keyboard.maxColCount * Keyboard.maxRowCount){
            this.setI32(i * 4, effect.getI32(i * 4));
        }
    }

    public function clearWithColor(color : Int){
        clearBaseColor(color);
        removeAllColorOverride();
    }

    public function clearBaseColor(color : Int){
        //Set Base Color
        for( i in 0 ... Keyboard.maxColCount * Keyboard.maxRowCount){
            this.setI32(i * 4, Utilities.swapColor(color));
        }
    }

    public function removeAllColorOverride(){
        //Set Perma Color (remove all override)
        for( i in Keyboard.maxColCount * Keyboard.maxRowCount ... Keyboard.maxColCount * Keyboard.maxRowCount * 2){
                this.setI32(i * 4, 0);
        }
    }

    public function setBaseColor(row : Int, column : Int, color : Int)
    {
        var index = row * Keyboard.maxColCount + column;
        if(index < Keyboard.maxColCount * Keyboard.maxRowCount){
            this.setI32(index * 4, 0x01000000 | Utilities.swapColor(color));
        }
    }

    public function setPermaColor(row : Int, column : Int, color : Int){
        var index = row * Keyboard.maxColCount + column + Keyboard.maxColCount * Keyboard.maxRowCount;
        if(index < Keyboard.maxColCount * Keyboard.maxRowCount + Keyboard.maxColCount * Keyboard.maxRowCount){
            this.setI32(index * 4, 0x01000000 | Utilities.swapColor(color));
        }
    }
}

class Keypad{
    public static inline var maxRowCount = 4;
    public static inline var maxColCount = 5;
}

abstract KeypadCustomEffect(hl.Bytes){
    public function new(){
        this = new hl.Bytes(Keypad.maxRowCount * Keypad.maxColCount * 4);
    }

    public function clearWithColor(color : Int){
        for( i in 0 ... Keypad.maxColCount * Keypad.maxRowCount){
            this.setI32(i * 4, Utilities.swapColor(color));
        }
    }

    public function setColor(row : Int, column : Int, color : Int){
        var index = row * Keypad.maxColCount + column;
        if(index < Keypad.maxColCount * Keypad.maxRowCount){
            this.setI32(index * 4, Utilities.swapColor(color));
        }
    }
}

class Mouse{
    public static inline var maxRowCount = 9;
    public static inline var maxColCount = 7;

}

abstract MouseCustomEffect(hl.Bytes)
{
    public function new(){
        this = new hl.Bytes(Mouse.maxRowCount * Mouse.maxColCount * 4);
    }

    public function clearWithColor(color : Int){
        for( i in 0 ... Mouse.maxColCount * Mouse.maxRowCount){
            this.setI32(i * 4, Utilities.swapColor(color));
        }
    }

    public function setColor(row : Int, column : Int, color : Int){
        var index = row * Keyboard.maxColCount + column;
        if(index < Mouse.maxColCount * Mouse.maxRowCount){
            this.setI32(index * 4, Utilities.swapColor(color));
        }
    }
}

class Headset{
    public static inline var maxLedsCount = 5;
}

abstract HeadsetCustomEffect(hl.Bytes)
{
    public function new(){
        this = new hl.Bytes(Headset.maxLedsCount * 4);
    }

    public function clearWithColor(color : Int){
        for( i in 0 ... Headset.maxLedsCount){
            this.setI32(i * 4, Utilities.swapColor(color));
        }
    }

    public function setColor(index : Int, color : Int){
        if(index < Headset.maxLedsCount){
            this.setI32(index * 4, Utilities.swapColor(color));
        }
    }
}

class Mousepad{
    public static inline var maxLedsCount = 15;
}

abstract MousepadCustomEffect(hl.Bytes)
{
    public function new(){
        this = new hl.Bytes(Mousepad.maxLedsCount* 4);
    }

    public function clearWithColor(color : Int){
        for( i in 0 ... Mousepad.maxLedsCount){
            this.setI32(i * 4, Utilities.swapColor(color));
        }
    }

    public function setColor(index : Int, color : Int){
        if(index < Mousepad.maxLedsCount){
            this.setI32(index * 4, Utilities.swapColor(color));
        }
    }
}

class ChromaLink{
    public static inline var maxLedsCount = 5;
}

abstract ChromaLinkCustomEffect(hl.Bytes)
{
    public function new(){
        this = new hl.Bytes(ChromaLink.maxLedsCount* 4);
    }

    public function clearWithColor(color : Int){
        for( i in 0 ... ChromaLink.maxLedsCount){
            this.setI32(i * 4, Utilities.swapColor(color));
        }
    }

    public function setColor(index : Int, color : Int){
        if(index < ChromaLink.maxLedsCount){
            this.setI32(index * 4, Utilities.swapColor(color));
        }
    }
}

@:hlNative("chroma")
class Api{
    @:hlNative("chroma", "init")
    public static function init(){
    }

    @:hlNative("chroma", "release")
    public static function release(){
    }

    @:hlNative("chroma", "setMouseEffect")
    public static function setMouseEffect(effect : MouseCustomEffect){}

    @:hlNative("chroma", "setKeyboardEffect")
    public static function setKeyboardEffect(effect : KeyboardCustomEffect){}

    @:hlNative("chroma", "setMousepadEffect")
    public static function setMousepadEffect(effect : MousepadCustomEffect){}

    @:hlNative("chroma", "setKeypadEffect")
    public static function setKeypadEffect(effect : KeypadCustomEffect){}

    @:hlNative("chroma", "setHeadsetEffect")
    public static function setHeadsetEffect(effect : HeadsetCustomEffect){}

    @:hlNative("chroma", "setKeyboardKeysEffect")
    public static function setKeyboardKeysEffect(effect : KeyboardCustomKeyEffect){}

    @:hlNative("chroma", "setLinkedEffect")
    public static function setLinkedEffect(effect :ChromaLinkCustomEffect){}
}