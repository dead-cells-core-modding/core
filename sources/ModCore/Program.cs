using System.Runtime.InteropServices;

namespace ModCore
{
    class Program
    {

        static void Main(string[] args)
        {

        }

        static int InjectMain(IntPtr args, int argsSize)
        {
            try
            {
                Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.RuntimeIdentifier}");

                Console.WriteLine("Initalizing");

                DeadCells.Initalize();

                Console.WriteLine("Loaded modding core");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(-1);
            }
            return 0;
        }
    }
}
