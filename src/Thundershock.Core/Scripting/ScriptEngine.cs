using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Esprima.Ast;
using Jint;
using Jint.Native;
using Thundershock.Core.Debugging;
using TypeReference = Jint.Runtime.Interop.TypeReference;

namespace Thundershock.Core.Scripting
{
    /// <summary>
    /// Provides low-level access to the Thundershock JavaScript scripting engine.
    /// </summary>
    public class ScriptEngine
    {
        private Engine _engine;
        private CancellationTokenSource _cancellationTokenSource;
        
        public ScriptEngine()
        {
            StaticInit();

            _cancellationTokenSource = new CancellationTokenSource();
            _engine = new Engine(Preconfigure);

            DefineCoreMethods();
        }

        public bool IsDefined(string name)
        {
            return _engine.GetValue(name) != JsValue.Undefined;
        }
        
        private void Preconfigure(Options options)
        {
            // Limit how much memory a script can allocate. Scripts should not be doing
            // memory-intensive tasks, that should be left to C#.
            // options.LimitMemory(4000000);
            
            // Limit how much time scripts are allowed to spend executing statements.
            // Scripts should not be able to halt the  engine loop.
            // options.TimeoutInterval(TimeSpan.FromSeconds(4));
            
            // This allows us to terminate scripts at will, for example on an engine crash
            // or when a new scene loads.
            options.CancellationToken(_cancellationTokenSource.Token);

            // Put JavaScript into strict mode.
            options.Strict();
            
            // Put JS in debug or release mode depending on how thundershock has been compiled.
            #if DEBUG
            options.DebugMode(true);
            #else
            options.DebugMode(false);
            #endif

            // This requires testing: Limit the levels of recursion allowed in scripts.
            // This prevents scripts from blowing up the stack.
            options.LimitRecursion(512);
            
            // Allow access to the CLR.
            options.AllowClr(this.GetType().Assembly);
        }

        public void ExecuteScript(string sourceCode)
        {
            _engine.Execute(sourceCode);
        }

        public void ExecuteStream(Stream file)
        {
            using var reader = new StreamReader(file);

            var script = reader.ReadToEnd();

            ExecuteScript(script);

            reader.Close();
        }

        public void ExecuteFile(string file)
        {
            using var stream = File.OpenRead(file);
            
            ExecuteStream(stream);

            stream.Close();
        }
        
        public void CallIfDefined(string name, params object[] args)
        {
            var values = args.Select(x => JsValue.FromObject(_engine, x)).ToArray();

            var method = _engine.Invoke(name, values);
        }

        public void CallIfDefined(string name)
            => _engine.Invoke(name);

        private void DefineCoreMethods()
        {
            // Expose script libraries.
            foreach (var lib in _libraries)
            {
                _engine.SetValue(lib.Name, lib.Reference);
            }

            foreach (var type in _types)
            {
                var actualType = type.Type;
                var name = type.Name;

                _engine.SetValue(name, TypeReference.CreateTypeReference(_engine, actualType));
            }
            
            // Here's where we get to define core global methods that all scripts must have access to.
            // These are mostly util functions like debug logging, and stuff like that.
            
            // Expose the Logger to scripts.
            var logger = Logger.GetLogger();
            _engine.SetValue("Logger", logger);
            
            // Log level aliases.
            _engine.SetValue("INFO", LogLevel.Info);
            _engine.SetValue("WARN", LogLevel.Warning);
            _engine.SetValue("ERROR", LogLevel.Error);
            _engine.SetValue("FATAL", LogLevel.Fatal);
            _engine.SetValue("TRACE", LogLevel.Trace);
            _engine.SetValue("MESSAGE", LogLevel.Message);
            
            // Short-hands for logging.
            _engine.Execute("function log(value) { Logger.Log(value.toString()); }");
            _engine.Execute("function warn(value) { Logger.Log(value.toString(), WARN); }");
            _engine.Execute("function error(value) { Logger.Log(value.toString(), ERROR); }");
            _engine.Execute("function fatal(value) { Logger.Log(value.toString(), FATAL); }");
        }

        public void SetGlobal(string name, object value)
            => _engine.SetValue(name, value);


        #region Static stuff

        private static bool _hasInitialized = false;
        private static List<ScriptLibrary> _libraries = new List<ScriptLibrary>();
        private static List<ScriptLibrary> _types = new();
        
        public static void StaticInit()
        {
            if (!_hasInitialized)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var types = Array.Empty<Type>();

                    try
                    {
                        types = asm.GetTypes();
                    }
                    catch (Exception ex)
                    {
                        Logger.GetLogger().LogException(ex);
                    }

                    foreach (var type in types)
                    {
                        var scriptType = type.GetCustomAttributes(true).OfType<ScriptTypeAttribute>().FirstOrDefault();
                        var scriptLibAttrib = type.GetCustomAttributes(false).OfType<ScriptStaticLibraryAttribute>()
                            .FirstOrDefault();

                        if (scriptLibAttrib != null)
                        {
                            if (type.GetConstructor(Type.EmptyTypes) == null)
                                continue;

                            if (string.IsNullOrWhiteSpace(scriptLibAttrib.Name))
                                continue;

                            var obj = Activator.CreateInstance(type, null);

                            _libraries.Add(new ScriptLibrary(scriptLibAttrib.Name, type, obj));
                            continue;
                        }

                        if (scriptType != null)
                        {
                            var name = scriptType.Name;
                            if (string.IsNullOrWhiteSpace(name))
                                name = type.Name;

                            var library = new ScriptLibrary(name, type, null);

                            _types.Add(library);
                        }
                    }
                }
                
                _hasInitialized = true;
            }
        }

        private class ScriptLibrary
        {
            public string Name { get; }
            public Type Type { get; }
            public object Reference { get; }

            public ScriptLibrary(string name, Type type, object reference)
            {
                Name = name;
                Type = type;
                Reference = reference;
            }
        }
        
        #endregion
    }
}