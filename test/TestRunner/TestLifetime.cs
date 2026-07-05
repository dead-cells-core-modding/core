using System;
using System.Collections.Generic;
using System.Text;
using TestRunner;

[assembly: AssemblyFixture(typeof(TestLifetime))]

namespace TestRunner
{
    internal class TestLifetime : IAsyncLifetime
    {
        private GameContext? gameContext;
        public ValueTask DisposeAsync()
        {
            return default;
        }

        public ValueTask InitializeAsync()
        {
            gameContext = new();
            return default;
        }
    }
}
