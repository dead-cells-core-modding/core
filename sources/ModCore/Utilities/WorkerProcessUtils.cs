using System.Diagnostics;

namespace ModCore.Utilities
{
    /// <summary>
    /// Provides utility methods for starting and managing worker processes that execute specified static methods in
    /// separate process contexts.
    /// </summary>
    /// <remarks>WorkerProcessUtils enables launching worker processes with configurable startup parameters,
    /// allowing execution of static methods in designated types. The utilities facilitate inter-process coordination by
    /// passing configuration through environment variables. All methods are static and thread-safe.</remarks>
    public static class WorkerProcessUtils
    {
        /// <summary>
        /// Starts a new worker process that executes a specified static method in a given type, passing configuration
        /// via environment variables.
        /// </summary>
        /// <remarks>The worker process is started with environment variables that specify the target
        /// type, method, and additional assemblies to load. The process will exit when the parent process terminates.
        /// The method requires that the specified type and method exist and are accessible in the worker process
        /// context.</remarks>
        /// <param name="typeFullName">The fully qualified name of the type containing the static method to invoke in the worker process. Cannot be
        /// null or empty.</param>
        /// <param name="methodName">The name of the static method to execute in the worker process. Cannot be null or empty.</param>
        /// <param name="startInfo">The process start information to use when launching the worker process. If null, a default configuration is
        /// used.</param>
        /// <param name="loadAssemblies">A list of assembly paths to be loaded by the worker process before invoking the specified method. Each entry
        /// should be a valid assembly path.</param>
        /// <returns>A Process instance representing the started worker process.</returns>
        public static Process StartWorkerProcess(string typeFullName, 
            string methodName,
            ProcessStartInfo? startInfo,
            params ReadOnlySpan<string> loadAssemblies)
        {
            if (ContextConfig.Config.disableWorkerProcessUtils)
            {
                throw new InvalidOperationException();
            }

            startInfo ??= new();

            if (string.IsNullOrEmpty(startInfo.FileName))
            {
                startInfo.FileName = Environment.ProcessPath;
                startInfo.Arguments = "";
            }
            startInfo.Environment["DCCM_CUSTOM_STARTUP_TYPE"] = typeFullName;
            startInfo.Environment["DCCM_CUSTOM_STARTUP_METHOD"] = methodName;
            startInfo.Environment["DCCM_EXIT_WHEN_PROCESS_PID"] = Environment.ProcessId.ToString();
            startInfo.Environment["DCCM_SHOULD_WAIT_FOR_DEBUGGER"] = null;
            startInfo.Environment["DCCM_LOAD_ADDITIONAL_ASSEMBLIES"] = string.Join(';', loadAssemblies);

            return Process.Start(startInfo)!;
        }
    }
}
