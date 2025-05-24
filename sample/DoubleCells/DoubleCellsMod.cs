using dc.en.hero;
using Hashlink;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Mods;
using ModCore.Modules;

namespace DoubleCells
{
    public class DoubleCellsMod(ModInfo info) : ModBase(info),
        IOnHeroUpdate
    {
        private double timeDelt = 0;

        private void Hook_beheaded_addMoney(Hook_Beheaded.orig_addMoney orig, Beheaded self, int val, Ref<bool> noStats)
        {
            val *= 200;
            orig(self, val, noStats);
        }
        private void Hook_beheaded_addCells(Hook_Beheaded.orig_addCells orig, Beheaded self, int val, Ref<bool> noStats)
        {
            val *= 200;
            orig(self, val, noStats);
        }
        public override void Initialize()
        {
            Logger.Information("Hello, World!");

            Hook_Beheaded.addMoney += Hook_beheaded_addMoney;
            Hook_Beheaded.addCells += Hook_beheaded_addCells;
        }

        void IOnHeroUpdate.OnHeroUpdate(double dt)
        {
            timeDelt += dt;
            var hero = Game.Instance.HeroInstance;
            if (timeDelt < 0.25f || hero == null)
            {
                return;
            }
            
            var curLife = hero.life;
            var maxLife = hero.maxLife;
            if (curLife < maxLife)
            {
                curLife += 1;
                hero.setLifeAndRally(curLife, 1);
            }
            timeDelt = 0;
        }
    }
}
