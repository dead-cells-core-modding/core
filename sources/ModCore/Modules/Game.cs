using ModCore.Modules.Events;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static SDL2.SDL;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class Game : CoreModule<Game>, IOnModCoreInjected
    {

        public override int Priority => ModulePriorities.Game;

        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
        private delegate IntPtr SDL_CreateWindow_Handler(
            byte* title,
            int x,
            int y,
            int w,
            int h,
            SDL_WindowFlags flags
        );
        private static SDL_CreateWindow_Handler orig_SDL_CreateWindow = null!;
        private static IntPtr Hook_SDL_CreateWindow(
            byte* title,
            int x,
            int y,
            int w,
            int h,
            SDL_WindowFlags flags
        )
        {

            var result = orig_SDL_CreateWindow(title, x, y, w, h, flags);
            Instance.MainWindowPtr = result;
            SDL.SDL_SetWindowTitle(result, "Dead Cells with Core Modding");
            return result;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate nint SDL_CreateWindowFrom_Handler(nint data);
        private static SDL_CreateWindowFrom_Handler orig_SDL_CreateWindowFrom = null!;
        private static nint Hook_SDL_CreateWindowFrom(nint data)
        {
            return 0;
        }

        public nint MainWindowPtr { get; private set; }

        void IOnModCoreInjected.OnModCoreInjected()
        {
            var sdl2 = NativeLibrary.Load("SDL2");

            orig_SDL_CreateWindow = NativeHook.Instance.CreateHook<SDL_CreateWindow_Handler>(
                NativeLibrary.GetExport(sdl2, "SDL_CreateWindow"),
                Hook_SDL_CreateWindow
                );
            orig_SDL_CreateWindowFrom = NativeHook.Instance.CreateHook<SDL_CreateWindowFrom_Handler>(
                NativeLibrary.GetExport(sdl2, "SDL_CreateWindowFrom"),
                Hook_SDL_CreateWindowFrom
                );

            SDL_CreateWindow("TAT", 0, 0, 2, 2, SDL_WindowFlags.SDL_WINDOW_SHOWN);
        }
    }
}
