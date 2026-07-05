using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ModCore.Utilities
{
    internal static class ProcessUtils
    {
        public static Process RedirectOutputToLogger( this Process process, ILogger logger )
        {
            process.ErrorDataReceived += ( _, e ) => logger.Error("{err}", e.Data);
            process.OutputDataReceived += ( _, e ) => logger.Information("{inf}", e.Data);
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            return process;
        }
    }
}
