using System;
using System.IO;
using System.Linq;
using System.Text;
using BinaryPack;
using Cairo;
using Thundershock.Core;
using Thundershock.Debugging;
using Thundershock.Gui;
using Thundershock.IO;

namespace Thundershock.Content
{
    public sealed class PakManager : GlobalComponent
    {
        private FileSystem _pakVfs;
        private PakRootNode _rootNode;

        protected override void OnLoad()
        {
            // Set up the root PakFS node.
            App.Logger.Log("PakManager is booting up.");
            App.Logger.Log("Creating the root PakFS node...");
            _rootNode = new PakRootNode();
            _pakVfs = FileSystem.FromNode(_rootNode);
            App.Logger.Log("PakFS created.");

            App.Logger.Log("Attempting to mount thundershock.pak ...");
            if (!TryMountGamePak("engine", "thundershock.pak"))
            {
                DialogBox.ShowError("Thundershock Engine",
                    "MISSING THUNDERSHOCK PAK FILE! Thundershock Engine cannot start. Your game installation might be corrupt. Please try reinstalling the game.");
                App.Exit();
            }

            base.OnLoad();
        }

        private bool TryMountGamePak(string vfsName, string file)
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
            App.Logger.Log("mounting: " + file);

            if (!File.Exists(path))
            {
                App.Logger.Log("FAILED.", LogLevel.Error);
                App.Logger.Log("File not found.", LogLevel.Error);
                return false;
            }

            try
            {
                var node = PakUtils.OpenPak(path);

                _rootNode.AddPak(vfsName, node);
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.Log("FAILED.", LogLevel.Error);
                App.Logger.LogException(ex);

                return false;
            }
        }

    }
}