
class TestShader extends hxsl.Shader
{
    static var SRC = {
        @input var input : { position : Vec3, normal : Vec3 };
        var output : { position : Vec4, normal : Vec3, color : Vec4 };
        var transformedNormal : Vec3;
        @param var materialColor : Vec4;
        @param var transformMatrix : Mat4;
        function vertex() {
            output.position = vec4(input.position,1.) * transformMatrix;
            transformedNormal = normalize(input.normal * mat3(transformMatrix));
        }
        function fragment() {
            output.color = materialColor;
            output.normal = transformedNormal;
        }
    };
}
