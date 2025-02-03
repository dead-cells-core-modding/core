using Serilog;

namespace LibraryMod
{
    public class HelloWorld
    {
        public static void Say()
        {
            Log.Information("Hello, World!This is a library!");
        }
    }
}
