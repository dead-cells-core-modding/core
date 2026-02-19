using dc.ui;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Menu
{
    public interface IModMenu
    {
        public string? GetSubText() => null;
        public string GetName();
        public void BuildMenu( Options options );
    }
}
