using Steamworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool
{
    internal static class CallResultUtils
    {
        private static bool steamShutdown = false;
        public static void StartLoop()
        {
            steamShutdown = false;
            Task.Factory.StartNew(() =>
            {
                while (!steamShutdown)
                {
                    SteamAPI.RunCallbacks();
                    Thread.Sleep(10);
                }
            }, TaskCreationOptions.LongRunning);
        }
        public static void StopLoop()
        {
            steamShutdown = true;
        }
        public static Task<T> Wait<T>(this SteamAPICall_t callback)
        {
            TaskCompletionSource<T> source = new(TaskCreationOptions.None);
            var cr = CallResult<T>.Create();
            cr.Set(callback, (result, failed) =>
            {
                cr.Dispose();
                Task.Factory.StartNew(() =>
                {
                    if (failed)
                    {
                        source.SetException(new Exception());
                    }
                    else
                    {
                        source.SetResult(result);
                    }
                });
            });
            return source.Task;
        }

    }
}