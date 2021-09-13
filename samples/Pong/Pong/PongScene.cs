using System;
using System.Numerics;
using Thundershock;
using Thundershock.Components;
using Thundershock.Core;
using Thundershock.Core.Input;
using Thundershock.Gui;
using Thundershock.Gui.Elements;
using Thundershock.Rendering;

namespace Pong
{
    public sealed class PongScene : Scene
    {
        #region Constants

        private const float BallSize = 40;
        private const float PaddleWidth = 26;
        private const float PaddleHeight = 180;
        private const float PaddlePadding = 100;

        #endregion

        #region Game State

        private float _cpuMovement;
        private int _cpuScore = 0;
        private int _playerScore = 0;
        private double _timeSinceLastReaction = 0;
        private double _reactionTime = 0.5;
        private float _cpuSpeed = 69;
        private float _ballXSpeed = 72f;
        private float _ballYSpeed = 72f;
        private float _ballSpeedXIncrease = 6f;
        private float _ballSpeedYIncrease = 3f;
        private Transform2D _cpu = new();
        private Transform2D _player = new();
        private Sprite _playerSprite = new();
        private Sprite _cpuSprite = new();
        private Transform2D _ball = new();
        private Sprite _ballSprite = new();
        private bool _paused = true;
        private bool _countingDown = false;
        private double _countdown = 0;
        
        #endregion

        #region Start Screen UI

        private Panel _startPanel = new();
        private Stacker _startStacker = new();
        private TextBlock _title = new();
        private TextBlock _gameInstructions = new();
        private Button _startButton = new();
        
        #endregion

        #region Countdown UI

        private Stacker _countdownGui = new();
        private TextBlock _countdownTitle = new();
        private TextBlock _countdownText = new();

        #endregion

        #region Score texts

        private TextBlock _playerScoreText = new();
        private TextBlock _cpuScoreText = new();

        #endregion
        
        protected override void OnLoad()
        {
            // Set up the required camera settings.
            PrimaryCameraSettings.SkyColor = Color.FromHtml("#222222");
            PrimaryCameraSettings.ProjectionType = CameraProjectionType.Orthographic;
            PrimaryCameraSettings.OrthoWidth = 1600;
            PrimaryCameraSettings.OrthoHeight = 900;

            // Sets the ball size.
            _ballSprite.Size = new Vector2(BallSize, BallSize);
            
            // Set paddle sizes
            _cpuSprite.Size = new Vector2(PaddleWidth, PaddleHeight);
            _playerSprite.Size = new Vector2(PaddleWidth, PaddleHeight);

            // Spawn the ball entity.
            var ballEntity = Registry.Create();
            Registry.AddComponent(ballEntity, _ball);
            Registry.AddComponent(ballEntity, _ballSprite);
            
            // Spawn in the paddles
            var cpuEntity = Registry.Create();
            var playerEntity = Registry.Create();
            
            Registry.AddComponent(cpuEntity, _cpu);
            Registry.AddComponent(playerEntity, _player);
            
            Registry.AddComponent(cpuEntity, _cpuSprite);
            Registry.AddComponent(playerEntity, _playerSprite);

            var viewBounds = GetViewBounds();

            _cpu.Position = new Vector2(viewBounds.Center.X - PaddlePadding, 0);
            _player.Position = new Vector2(-viewBounds.Center.X + PaddlePadding, 0);
            
            InputSystem.MouseMove += InputSystemOnMouseMove;

            _startStacker.VerticalAlignment = VerticalAlignment.Center;
            _startStacker.HorizontalAlignment = HorizontalAlignment.Center;

            _startStacker.Children.Add(_title);
            _startStacker.Children.Add(_gameInstructions);
            _startStacker.Children.Add(_startButton);

            _title.Text = "Welcome to Pong!";
            _gameInstructions.Text =
                @"Your goal is to keep the ball from getting past youre paddle while trying to get it past the other player's paddle. If you get the ball past the opponent's paddle, you get a point. If the opponent gets the ball past you, they get a point.

Use the mouse to move your paddle up and down to hit the ball.

The first player to get 30 points wins.";

            _startButton.Text = "Let's start!";
            
            _title.TextAlign = TextAlign.Center;
            _gameInstructions.TextAlign = TextAlign.Center;

            _title.Padding = 30;
            _startButton.Padding = 60;
            
            _startPanel.Children.Add(_startStacker);

            _countdownTitle.Padding = 200;

            _countdownTitle.ForeColor = Color.White;
            _countdownText.ForeColor = Color.White;

            _countdownTitle.TextAlign = TextAlign.Center;
            _countdownText.TextAlign = TextAlign.Center;
            
            _countdownGui.Children.Add(_countdownTitle);
            _countdownGui.Children.Add(_countdownText);

            Gui.AddToViewport(_playerScoreText);
            Gui.AddToViewport(_cpuScoreText);
            Gui.AddToViewport(_countdownGui);
            Gui.AddToViewport(_startPanel);

            _title.ForeColor = Color.White;
            _gameInstructions.ForeColor = Color.White;

            _startStacker.FixedWidth = 640;
            
            _startPanel.BackColor = Color.Black;
            
            _startButton.MouseUp += StartButtonOnMouseUp;

            _playerScoreText.ViewportAlignment = new Vector2(0.5f, 0.5f);
            _cpuScoreText.ViewportAlignment = new Vector2(0.5f, 0.5f);

            _playerScoreText.ForeColor = Color.White;
            _cpuScoreText.ForeColor = Color.White;
            
            _playerScoreText.ViewportAnchor = new FreePanel.CanvasAnchor(0.25f, 0.05f, 0, 0);
            _cpuScoreText.ViewportAnchor = new FreePanel.CanvasAnchor(0.75f, 0.05f, 0, 0);
        }

        private void StartButtonOnMouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Primary)
            {
                _startPanel.Visibility = Visibility.Collapsed;
                _countingDown = true;
                _countdownTitle.Text = "Ready to play?";
                _countdown = 3;
            }
        }

        private void InputSystemOnMouseMove(object? sender, MouseMoveEventArgs e)
        {
            var viewBounds = GetViewBounds();
            var screenBounds = ViewportBounds;

            var y = e.Y / screenBounds.Height;
            y *= viewBounds.Height;
            y -= viewBounds.Center.Y;

            _player.Position = new Vector2(_player.Position.X, y);
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            if (_countingDown)
            {
                if (_countdown <= 0)
                {
                    _countingDown = false;
                    _paused = false;
                }

                _countdownText.Text = Math.Round(_countdown).ToString();
                _countdown -= gameTime.ElapsedGameTime.TotalSeconds;
            }

            _countdownGui.Visibility = _countingDown ? Visibility.Visible : Visibility.Collapsed;

            _playerScoreText.Text = _playerScore.ToString();
            _cpuScoreText.Text = _cpuScore.ToString();
            
            if (_paused) return;
            
            _reactionTime += gameTime.ElapsedGameTime.TotalSeconds;
            
            var screenBounds = ViewportBounds;
            var ballRect = GetScreenSpaceBounds(_ball, _ballSprite);
            var playerRect = GetScreenSpaceBounds(_player, _playerSprite);
            var cpuRect = GetScreenSpaceBounds(_cpu, _cpuSprite);

            if (ballRect.IntersectsWith(playerRect))
            {
                var bPos = _ball.Position;
                var pPos = _player.Position;
                bPos.X = pPos.X;
                bPos.X += _playerSprite.Size.X / 2;
                bPos.X += _ballSprite.Size.X / 2;
                _ballXSpeed = Math.Abs(_ballXSpeed) + _ballSpeedXIncrease;

                _ball.Position = bPos;
            }
            
            if (ballRect.IntersectsWith(cpuRect))
            {
                var bPos = _ball.Position;
                var pPos = _cpu.Position;
                bPos.X = pPos.X;
                bPos.X -= _cpuSprite.Size.X / 2;
                bPos.X -= _ballSprite.Size.X / 2;
                _ballXSpeed = -Math.Abs(_ballXSpeed) - _ballSpeedXIncrease;

                _ball.Position = bPos;
            }
            
            if (ballRect.Bottom >= screenBounds.Bottom || ballRect.Top <= screenBounds.Top)
            {
                _ballYSpeed = -_ballYSpeed;

                if (_ballYSpeed < 0)
                    _ballYSpeed -= _ballSpeedYIncrease;
                else
                    _ballYSpeed += _ballSpeedYIncrease;
            }

            // CPU is beaten
            if (ballRect.Left >= screenBounds.Right)
            {
                _ballXSpeed = -_ballXSpeed;
                _playerScore++;
                _ball.Position = Vector2.Zero;

                _paused = true;
                _countingDown = true;
                _countdown = 3;
                _countdownTitle.Text = "You got a point!";
            }
            
            // Player is beat.
            if (ballRect.Right <= screenBounds.Left)
            {
                _ballXSpeed = -_ballXSpeed;
                _cpuScore++;
                _ball.Position = Vector2.Zero;

                _paused = true;
                _countingDown = true;
                _countdown = 3;
                _countdownTitle.Text = "CPU gets a point!";
            }

            var ballPos = _ball.Position;
            var cpuPos = _cpu.Position;
            ballPos += new Vector2(_ballXSpeed, _ballYSpeed) 
                       * (float) gameTime.ElapsedGameTime.TotalSeconds;
            _ball.Position = ballPos;

            if (_reactionTime >= _timeSinceLastReaction)
            {
                var cpuDistance = ballPos.Y - cpuPos.Y;

                if (ballPos.X > 0 && _ballXSpeed >= 0)
                {
                    _cpuMovement = MathHelper.Clamp(cpuDistance, -_cpuSpeed, _cpuSpeed) *
                                   (float) gameTime.ElapsedGameTime.TotalSeconds;
                }
                else
                {
                    _cpuMovement = 0;
                }
                
                _reactionTime = 0;
            }
            
            cpuPos.Y += _cpuMovement;
            _cpu.Position = cpuPos;
        }

        public Rectangle GetScreenSpaceBounds(Transform2D transform, Sprite sprite)
        {
            var size = sprite.Size * transform.Scale;

            var viewBounds = GetViewBounds();
            var screenBounds = ViewportBounds;
            
            var position = transform.Position - (size / 2);

            position += viewBounds.Center;
            position *= (viewBounds.Size / screenBounds.Size);
            
            size *= (viewBounds.Size / screenBounds.Size);

            return new Rectangle(position.X, position.Y, size.X, size.Y);

        }

        private Rectangle GetViewBounds()
        {
            return new Rectangle(0, 0, PrimaryCameraSettings.OrthoWidth, PrimaryCameraSettings.OrthoHeight);
        }
    }
}