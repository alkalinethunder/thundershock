using System.Collections.Generic;
using System.IO;
using Thundershock.Components;
using Thundershock.Core;
using Thundershock.Core.Scripting;

namespace Thundershock
{
    public class ScriptSystem : ISystem
    {
        private Scene _scene;
        
        public void Init(Scene scene)
        {
            _scene = scene;

        }

        public void Unload()
        {
        }

        public void Load()
        {
            // Load all scripts.
            LoadScripts();
        }

        public void Update(GameTime gameTime)
        {
            // Get all script runners.
            var runners = _scene.Registry.View<ScriptRunner>();
            
            // Look at all the entities with a runner on it
            foreach (var entity in runners)
            {
                // Get the runner
                var runner = _scene.Registry.GetComponent<ScriptRunner>(entity);
                
                // Tick it!
                runner.Update((float) gameTime.ElapsedGameTime.TotalSeconds);
            }
        }

        public void Render(GameTime gameTime)
        {
        }

        private void LoadScripts()
        {
            // Query all entities that have a script component.
            var scriptEntities = _scene.Registry.View<ScriptComponent>();

            // Look at all the entities in the query above
            foreach (var entity in scriptEntities)
            {
                // Retrieve the script component.
                var scriptComponent = _scene.Registry.GetComponent<ScriptComponent>(entity);
                
                // Try to open the script.
                if (TryOpenScript(scriptComponent, out var scriptStream))
                {
                    // Create a new script runner. The script runner takes care of sending update hooks
                    // along to JavaScript. It's the component we'll add to the entity automatically
                    // that we'll query later on to actually run the script.
                    var runner = new ScriptRunner(this, entity, scriptStream);
                    
                    // Close the script stream
                    scriptStream.Close();
                    
                    // Add the runner to the entity.
                    _scene.Registry.AddComponent(entity, runner);
                    
                    // Invoke the "Awake" function.
                    runner.Awake();
                }
            }
        }

        private bool TryOpenScript(ScriptComponent component, out Stream stream)
        {
            var path = component.Script;

            stream = Stream.Null;

            if (string.IsNullOrWhiteSpace(path))
                return false;
            
            // /!\ /!\ /!\ /!\ /!\ /!\ /!\ /!\
            // HUGE FUCKING DANGER CODE COMING!
            // /!\ /!\ /!\ /!\ /!\ /!\ /!\/!\
            //
            // This code takes the path stored in the script component
            // data and simply loads it directly from the user's hard drive!!!
            //
            // THIS IS FUCKING AWFUL!!! THAT'S HOW MALWARE GETS WRITTEN!!!
            //
            // But, while Pak support is still an infant feature, YOU TELL ME
            // what a better alternative is.
            //
            // I'm not fucking merging this branch until this works without
            // being a malware attack vector.
            //
            // - Michael
            if (File.Exists(path))
            {
                stream = File.OpenRead(path); // EWWWWWWWW EW EW EW EW!!!!!!
                return true;
            }
            
            return false;
        }

        private class ScriptRunner
        {
            private ScriptSystem _sys;
            private uint _entity;
            private ScriptEngine _script;

            public ScriptRunner(ScriptSystem sys, uint entity, Stream scriptStream)
            {
                _sys = sys;
                _entity = entity;
                _script = new ScriptEngine();

                _script.SetGlobal("Scene", _sys._scene);
                _script.SetGlobal("Gui", _sys._scene.Gui);
                
                _script.ExecuteStream(scriptStream);
            }

            public void Awake()
            {
                _script.CallIfDefined("awake");
            }

            public void Update(float deltaTime)
            {
                _script.CallIfDefined("update", deltaTime);
            }
            
            
        }
    }
}