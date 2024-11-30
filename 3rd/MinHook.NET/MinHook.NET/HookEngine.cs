using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static MinHook.Utils;

namespace MinHook {
    public sealed class HookEngine : IDisposable {

        MemoryAllocator memoryAllocator = new MemoryAllocator();
        List<IntPtr> suspendedThreads = new List<IntPtr>();
        List<Hook> hooks = new List<Hook>();
        public Hook CreateHook(nint target, nint detour)
        {
            if (target == nint.Zero || detour == nint.Zero)
            {
                throw new ArgumentException($"target or detour cannot be null");
            }

            lock (this)
            {
                var hook = new Hook(target, detour, memoryAllocator.AllocateBuffer(target));
                hooks.Add(hook);
                return hook;
            }
        }



        public void EnableHooks() {
            foreach(var hook in hooks) {
                EnableHook(hook);
            }
        }

        public void DisableHooks() {
            foreach (var hook in hooks) {
                DisableHook(hook);
            }
        }

        public void EnableHook(Hook hook)
        {
            lock (this)
            {
                SuspendThreads();
                hook.Enable(true);
                ResumeThreads();
            }
        }

        public void DisableHook(Hook hook)
        {
            lock (this)
            {
                SuspendThreads();
                hook.Enable(false);
                ResumeThreads();
            }
        }

        void SuspendThreads() {

            //Suspending all threads when debugging causes deadlocks.
            if (Debugger.IsAttached) {
                return;
            }

            //TODO: Currently doesn't move thread IP if any of the threads
            //are executing within the location of a hook prologue at the time.
            //This will probably crash the program if that scenario happens (rare)

            Process currentProc = Process.GetCurrentProcess();

            foreach(ProcessThread thread in currentProc.Threads) {
                if(thread.Id != GetCurrentThreadId()) {                    
                    IntPtr threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                    SuspendThread(threadHandle);
                    suspendedThreads.Add(threadHandle);                                       
                }             
            }
        }

        void ResumeThreads() {

            foreach(var handle in suspendedThreads) {
                ResumeThread(handle);
                CloseHandle(handle);                    
            }

            suspendedThreads.Clear();
        }

        public void Dispose() {
            DisableHooks();
            memoryAllocator.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
