﻿using Hashlink.Marshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMod;

namespace TestRunner
{
    public class ModLoaderTest
    {
        [Fact]
        public void ModLoader_LoadMod()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            Assert.True(TestModMain.modIsLoaded);
        }
    }
}
