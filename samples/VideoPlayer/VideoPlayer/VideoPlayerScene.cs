using System;
using System.Numerics;
using Thundershock;
using Thundershock.Components;
using Thundershock.Core;
using Thundershock.Core.Audio;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;
using Thundershock.Gui;
using Thundershock.Gui.Elements;
using Thundershock.Rendering;

namespace VideoPlayer
{
    public sealed class VideoPlayerScene : Scene
    {
        private Button _fileSelector = new();
        private Sprite _videoSprite = new();
        private Texture2D _videoTexture;
        private Transform2D _videoTransform = new();
        private AudioOutput _lowLevelAudioPlayer;
        
        protected override void OnLoad()
        {
            base.OnLoad();

            // Create the low-level audio player. You can recreate this as needed based on the video's audio format,
            // don't forget to stop and dispose the old one.
            _lowLevelAudioPlayer = GamePlatform.Audio.OpenAudioOutput();

            // Orthographic projection for 2D scenes. Perspective is borked.
            PrimaryCameraSettings.ProjectionType = CameraProjectionType.Orthographic;
            
            // 1080p viewport, you can change this
            PrimaryCameraSettings.OrthoWidth = 1920;
            PrimaryCameraSettings.OrthoHeight = 1080;

            // Create a 1080p texture for the video output. You can destroy and recreate the texture for
            // other video sizes, just make sure to set the new texture to be used by the sprite
            _videoTexture = new Texture2D(Graphics, 1920, 1080, TextureFilteringMode.Linear); // linear filtering for when the engine renders at a different resolution than what the video is encoded at
            
            // Spawn a 2D sprite for displaying the texture.
            var spriteEntity = Registry.Create();
            Registry.AddComponent(spriteEntity, _videoTransform);
            Registry.AddComponent(spriteEntity, _videoSprite);

            // Assign the texture to the sprite. Make sure the sizes match.
            _videoSprite.Texture = _videoTexture;
            _videoSprite.Size = _videoTexture.Bounds.Size;
            
            // Set up the "select video" button
            _fileSelector.Text = "Select video file";
            _fileSelector.ViewportAnchor = FreePanel.CanvasAnchor.TopLeft;
            _fileSelector.ViewportPosition = new Vector2(25, 25);

            // Add the button to the UI for this scene.
            Gui.AddToViewport(_fileSelector);
            
            // React to the button mouseup event.
            _fileSelector.MouseUp += FileSelectorOnMouseUp;
        }

        private void FileSelectorOnMouseUp(object? sender, MouseButtonEventArgs e)
        {
            // Ask the user for an MP4 file to play  when the button is clicked.
            if (e.Button == MouseButton.Primary)
            {
                var chooser = new FileChooser();
                chooser.Title = "Select Video File";
                chooser.AcceptFileType(".mp4", "MP4 Video");
                chooser.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

                if (chooser.Activate() == FileOpenerResult.Ok)
                {
                    PlayVideo(chooser.SelectedFilePath);
                }
            }
        }

        private void PlayVideo(string path)
        {
            // Load the video file and set ourselves up for playback.
            DialogBox.ShowError("Not implemented", "You can't do this yet because this feature's not implemented.");
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            // This is where you should update the video output texture and submit more data to the audio output.
            base.OnUpdate(gameTime);
        }
    }
}