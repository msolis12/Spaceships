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
        private bool isGameOver = false;

        private void UpdatePlayerPosition(double deltaX)
        {
            var newX = PlayerShip.TranslationX + deltaX;

            // Limita el movimiento para que no se salga de la pantalla
            if (newX < 0)
            {
                // Si la nave intenta moverse más allá del borde izquierdo, fijarla en 0
                PlayerShip.TranslationX = 0;
            }
            else if (newX > (Width - PlayerShip.Width))
            {
                // Si la nave intenta moverse más allá del borde derecho, fijarla en el borde derecho
                PlayerShip.TranslationX = Width - PlayerShip.Width;
            }
            else
            {
                // Si la nave está dentro de los límites, actualiza la posición
                PlayerShip.TranslationX = newX;
            }
        }

        public MainPage()
        {
            InitializeComponent();
            _ = SetupGameAsync(); // Ejecuta el método SetupGame de forma asincrónica
        }

        private async Task SetupGameAsync()
        {

            // Inicializa el puntaje en 0
            score = 0;
            lblHighScore.Text = "HIGH SCORE: " + score;

            // Iniciar el audio del jugador
            var audioManager = AudioManager.Current;

            using var backgroundMusicStream = await FileSystem.OpenAppPackageFileAsync("515405__matrixxx__retro-gaming.wav");
            backgroundMusicPlayer = audioManager.CreatePlayer(backgroundMusicStream);

            using var collisionSoundStream = await FileSystem.OpenAppPackageFileAsync("745161__etheraudio__retro-death.wav");
            collisionSoundPlayer = audioManager.CreatePlayer(collisionSoundStream);

            // Iniciar la música de fondo
            backgroundMusicPlayer.Loop = true;
            backgroundMusicPlayer.Play();

            // Inicializar la nave aliada en el centro de la pantalla
            PlayerShip.TranslationX = (Width / 2) - (PlayerShip.Width / 2);
            PlayerShip.TranslationY = Height - PlayerShip.Height - 20;

            // Inicializar el diseño y los elementos del juego.
            StartGyroscope();
            gameTimer.Elapsed += OnGameTick;
            gameTimer.Start();

            // Generar naves enemigas
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
                // Se usa AngularVelocity.Y para mover la nave horizontalmente
                var deltaX = e.Reading.AngularVelocity.Y * 10; // Ajusta el multiplicador según sea necesario

                // Actualizamos la posición de la nave aliada
                UpdatePlayerPosition(deltaX);
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
                if (HayColision(enemy) && PlayerShip.IsVisible) // Asegurarse de que la nave aliada es visible antes de chequear colisiones
                {
                    // Play collision sound
                    collisionSoundPlayer?.Play();

                    // Detener música de fondo
                    backgroundMusicPlayer?.Stop();

                    // Show explosion
                    PlayerShip.Source = "Resources/Images/Explosion/explosion.png";
                    PlayerShip.IsVisible = false; // Ocultar la nave después de la colisión

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

            // Comprobación para evitar colisiones tempranas
            if (PlayerShip.TranslationY < 0 || enemigoBoxView.TranslationY < 0)
            {
                return false; // Evita colisiones cuando alguna de las naves está fuera de la pantalla.
            }

            // Verificar si los rectángulos se intersectan
            return playerRect.IntersectsWith(enemyRect);
        }

        private void GameOver()
        {
            gameTimer.Stop();
            isGameOver = true; // Marcar que el juego ha terminado para detener la generación de naves enemigas

            Dispatcher.Dispatch(async () =>
            {
                await DisplayAlert("Game Over", "You've been hit!", "Restart");
                RestartGame();
            });
        }


        private void RestartGame()
        {
            Dispatcher.Dispatch(() =>
            {
                score = 0;
                isGameOver = false; // Reiniciar el estado de Game Over

                // Reiniciar la música de fondo
                backgroundMusicPlayer?.Stop();
                backgroundMusicPlayer?.Play();

                // Reaparecer y centrar la nave aliada
                PlayerShip.Source = "Resources/Images/Player/starship.png";
                PlayerShip.TranslationX = (Width / 2) - (PlayerShip.Width / 2);
                PlayerShip.TranslationY = Height - PlayerShip.Height - 20;

                // Asegurarse de que la nave es visible y habilitarla
                PlayerShip.IsVisible = true;
                PlayerShip.IsEnabled = true;

                EnemyShipsContainer.Children.Clear();
                gameTimer.Start();
                StartEnemyGeneration(); // Reanuda la generación de naves enemigas
            });
        }


        private void StartEnemyGeneration()
        {
            if (!isGameOver)
            {
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(1000), () =>
                {
                    GenerateEnemyShip();
                    // Continuar generando naves enemigas si el juego no ha terminado
                    if (!isGameOver)
                    {
                        StartEnemyGeneration(); // Llamada recursiva para seguir generando
                    }
                });
            }
        }

        private void GenerateEnemyShip()
        {
            var enemyShip = new Image
            {
                Source = "Resources/Images/Enemies/starshipdark.png",
                WidthRequest = 50,
                Rotation = 180,
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
                    // Remueve la nave enemiga después de esquivarla
                    EnemyShipsContainer.Children.Remove(enemyShip);

                    // Incrementa el puntaje por esquivar la nave
                    score += 20;
                    lblHighScore.Text = "HIGH SCORE: " + score;
                });
            });
        }

        private void UpdateScore()
        {
            score += 20;
            lblHighScore.Text = "HIGH SCORE: " + score;
        }
    }
}
