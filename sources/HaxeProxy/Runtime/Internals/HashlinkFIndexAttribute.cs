namespace HaxeProxy.Runtime.Internals
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HashlinkFIndexAttribute( int findex ) : Attribute
    {

        public int Index {
            get;
        } = findex;
    }
}
