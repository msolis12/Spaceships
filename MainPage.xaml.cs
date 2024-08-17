using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Space_ships
{
    public partial class MainPage : ContentPage
    {
        private System.Timers.Timer gameTimer = new System.Timers.Timer(100);
        private IAudioPlayer? backgroundMusicPlayer;
        private IAudioPlayer? collisionSoundPlayer;
        private int score = 0;

        public MainPage()
        {
            InitializeComponent();
            _ = SetupGameAsync(); // Ejecuta el método SetupGame de forma asincrónica
        }

        private async Task SetupGameAsync()
        {
            // Initialize the audio players
            var audioManager = AudioManager.Current;

            using var backgroundMusicStream = await FileSystem.OpenAppPackageFileAsync("515405__matrixxx__retro-gaming.wav");
            backgroundMusicPlayer = audioManager.CreatePlayer(backgroundMusicStream);

            using var collisionSoundStream = await FileSystem.OpenAppPackageFileAsync("745161__etheraudio__retro-death.wav");
            collisionSoundPlayer = audioManager.CreatePlayer(collisionSoundStream);

            // Start the background music
            backgroundMusicPlayer.Loop = true;
            backgroundMusicPlayer.Play();

            // Initialize the game layout and elements
            StartGyroscope();
            gameTimer.Elapsed += OnGameTick;
            gameTimer.Start();

            // Start generating enemy ships
            StartEnemyGeneration();
        }

        private void StartGyroscope()
        {
            if (Gyroscope.Default.IsSupported)
            {
                Gyroscope.Default.ReadingChanged += Gyroscope_ReadingChanged;
                Gyroscope.Default.Start(SensorSpeed.UI);
            }
        }

        private void Gyroscope_ReadingChanged(object? sender, GyroscopeChangedEventArgs e)
        {
            // Manejar los cambios en el giroscopio
            Dispatcher.Dispatch(() =>
            {
                var x = e.Reading.AngularVelocity.X;
                PlayerShip.TranslationX += x * 10; // Ajusta el multiplicador según sea necesario
            });
        }


        private void OnGameTick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // Lógica del juego en cada tick
            Dispatcher.Dispatch(() =>
            {
                CheckCollisions();
                UpdateScore();
            });
        }


        private void CheckCollisions()
        {
            foreach (var enemy in EnemyShipsContainer.Children.OfType<View>().ToList())
            {
                if (HayColision(enemy))
                {
                    // Play collision sound
                    collisionSoundPlayer?.Play();

                    // Show explosion
                    PlayerShip.Source = "Resources/Images/Explosion/explosion.png";

                    // Stop the game
                    GameOver();
                    break;
                }
            }
        }

        private bool HayColision(View enemigoBoxView)
        {
            var playerRect = new Rect(PlayerShip.TranslationX, PlayerShip.TranslationY, PlayerShip.Width, PlayerShip.Height);
            var enemyRect = new Rect(enemigoBoxView.TranslationX, enemigoBoxView.TranslationY, enemigoBoxView.Width, enemigoBoxView.Height);
            return playerRect.IntersectsWith(enemyRect);
        }

        private void GameOver()
        {
            gameTimer.Stop();
            // Implementa la lógica de Game Over, por ejemplo, mostrar una alerta y reiniciar el juego
            DisplayAlert("Game Over", "You've been hit!", "Restart").ContinueWith(t => RestartGame());
        }

        private void RestartGame()
        {
            Dispatcher.Dispatch(() =>
            {
                score = 0;
                PlayerShip.Source = "Resources/Images/Player/starship.png";
                PlayerShip.TranslationX = Width / 2 - PlayerShip.Width / 2;
                PlayerShip.TranslationY = Height - PlayerShip.Height - 20;
                EnemyShipsContainer.Children.Clear();
                gameTimer.Start();
            });
        }


        private void StartEnemyGeneration()
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(1000), () =>
            {
                GenerateEnemyShip();
            });
        }


        private void GenerateEnemyShip()
        {
            var enemyShip = new Image
            {
                Source = "Resources/Images/Enemies/starshipdark.png",
                WidthRequest = 50,
                HeightRequest = 50
            };

            Random rnd = new Random();
            double xPosition = rnd.NextDouble() * (Width - enemyShip.WidthRequest);
            double yPosition = -enemyShip.HeightRequest;

            AbsoluteLayout.SetLayoutBounds(enemyShip, new Rect(xPosition, yPosition, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
            EnemyShipsContainer.Children.Add(enemyShip);

            // Animate enemy ship movement
            enemyShip.TranslateTo(xPosition, Height, 5000, Easing.Linear).ContinueWith(t =>
            {
                Dispatcher.Dispatch(() =>
                {
                    EnemyShipsContainer.Children.Remove(enemyShip);
                });
            });
        }


        private void UpdateScore()
        {
            score += 1;
            lblHighScore.Text = "HIGH SCORE: " + score;
        }
    }
}
