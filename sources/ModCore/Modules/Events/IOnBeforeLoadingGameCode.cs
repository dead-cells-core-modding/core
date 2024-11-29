using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules.Events
{
    public interface IOnBeforeLoadingGameCode
    {
        public unsafe class GameCode
        {
            public byte* data;
            public int size;
        }
        void OnBeforeLoadingGameCode(GameCode code);
    }
}
