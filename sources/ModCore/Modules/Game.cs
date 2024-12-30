using Hashlink;
using ModCore.Events;
using ModCore.Hashlink;
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
    public unsafe class Game : CoreModule<Game>, IOnModCoreInjected, IOnBeforeGameStartup
    {
        private HashlinkHook hhook = null!;
        public override int Priority => ModulePriorities.Game;

        public int GameVersion { get; private set; }
        public nint MainWindowPtr { get; private set; }

        private object? Hook_Boot_update(HashlinkFunc orig, HashlinkObject self, double dt)
        {
            EventSystem.BroadcastEvent<IOnFrameUpdate, double>(dt);
            return orig.Call(self, dt);
        }
        private object? Hook_Boot_Init(HashlinkFunc orig, HashlinkObject self)
        {
            GameVersion = (int)HashlinkUtils.GetGlobal("Main").Dynamic.GAME_VERSION;

            EventSystem.BroadcastEvent<IOnGameInit>();
            return orig.Call(self);
        }
        private object? Hook_Boot_endInit(HashlinkFunc orig, HashlinkObject self)
        {
            MainWindowPtr = (nint)self.Dynamic.engine.window.window.win;

            Logger.Information("Game initialization completed");
            
            SDL_SetWindowTitle(MainWindowPtr, "Dead Cells with Core Modding");

            EventSystem.BroadcastEvent<IOnGameEndInit>();

            return orig.Call(self);
        }
        
        private object? Hook_Hero_onDamage(HashlinkFunc orig, HashlinkObject self, HashlinkObject damage)
        {
            var baseDmg = damage.Dynamic.baseDmg;
            damage.Dynamic.dmgMultiplier = 0.01f;
            var sm = (HashlinkObject) self.Dynamic.addCells;
            sm.Dynamic(
                -123, 
                new HashlinkObject(sm.HashlinkType->data.func->args[2]));
            return orig.Call(self, damage);
        }
        private object? Hook_Hero_addMoney(HashlinkFunc orig, HashlinkObject self, int v, HashlinkObject v2)
        {
            v += 100;
            self.Dynamic.cells += v;
            //self.Dynamic.substractMoney(v + 10, v2);
            return orig.Call(self, v, v2);
        }
        private object? Hook_Hero_addCells(HashlinkFunc orig, HashlinkObject self, int v, HashlinkObject v2)
        {
            v *= 100;
            return orig.Call(self, v, v2);
        }

        void IOnBeforeGameStartup.OnBeforeGameStartup()
        {
            hhook.CreateHook(HashlinkUtils.GetFunction("Boot", "update"), Hook_Boot_update);
            hhook.CreateHook(HashlinkUtils.GetFunction("Boot", "endInit"), Hook_Boot_endInit);
            hhook.CreateHook(HashlinkUtils.GetFunction("Boot", "init"), Hook_Boot_endInit);

            //hhook.CreateHook(HashlinkUtils.GetFunction("en.Hero", "onDamage"), Hook_Hero_onDamage);
            //hhook.CreateHook(HashlinkUtils.GetFunction("en.Hero", "addMoney"), Hook_Hero_addMoney);
            //hhook.CreateHook(HashlinkUtils.GetFunction("en.Hero", "addCells"), Hook_Hero_addCells);
        }

        void IOnModCoreInjected.OnModCoreInjected()
        {
            hhook = HashlinkHook.Instance;
        }
    }
}
