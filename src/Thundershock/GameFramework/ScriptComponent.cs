using System.Collections.Generic;
using Thundershock.Core;

namespace Thundershock.GameFramework
{
    public sealed class ScriptComponent
    {
        private List<Script> _scripts = new();


        public void AddScript(Script script)
        {
            _scripts.Add(script);
        }

        public void Update(GameTime gameTime)
        {
            foreach (var script in _scripts)
                script.OnUpdate(gameTime);
        }
    }
}