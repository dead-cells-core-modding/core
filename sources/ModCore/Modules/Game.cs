using Hashlink;
using ModCore.Hashlink;
using ModCore.Modules.Events;
using ModCore.Track;
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr SDL_CreateWindow_Handler(
            byte* title,
            int x,
            int y,
            int w,
            int h,
            SDL_WindowFlags flags
        );
        private static SDL_CreateWindow_Handler orig_SDL_CreateWindow = null!;
        [CallFromHLOnly]
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

  

        public nint MainWindowPtr { get; private set; }

        void IOnModCoreInjected.OnModCoreInjected()
        {
            var sdl2 = NativeLibrary.Load("SDL2");

            orig_SDL_CreateWindow = NativeHook.Instance.CreateHook<SDL_CreateWindow_Handler>(
                NativeLibrary.GetExport(sdl2, "SDL_CreateWindow"),
                Hook_SDL_CreateWindow
                );


        }
    }
}
