using FlappyBird.Enums;

namespace FlappyBird.Core
{
    /// <summary>
    /// Oyun ayarlarını ve zorluk seviyesi parametrelerini yöneten sınıf.
    /// </summary>
    public class GameSettings
    {
        public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Medium;

        public int PipeGap { get; private set; } = 145;
        public float BasePipeSpeed { get; private set; } = 3.6f;
        public float Gravity { get; private set; } = 0.42f;
        public float JumpStrength { get; private set; } = -8.2f;
        public int PipeSpacing { get; private set; } = 220;

        public GameSettings()
        {
            ApplyDifficulty(Difficulty.Medium);
        }

        /// <summary>
        /// Seçilen zorluk seviyesine göre fiziksel oyun dinamiklerini ayarlar.
        /// </summary>
        public void ApplyDifficulty(Difficulty difficulty)
        {
            CurrentDifficulty = difficulty;

            switch (difficulty)
            {
                case Difficulty.Easy:
                    PipeGap = 175;
                    BasePipeSpeed = 2.8f;
                    Gravity = 0.38f;
                    JumpStrength = -7.5f;
                    PipeSpacing = 240;
                    break;

                case Difficulty.Medium:
                    PipeGap = 145;
                    BasePipeSpeed = 3.6f;
                    Gravity = 0.42f;
                    JumpStrength = -8.2f;
                    PipeSpacing = 220;
                    break;

                case Difficulty.Hard:
                    PipeGap = 120;
                    BasePipeSpeed = 4.4f;
                    Gravity = 0.48f;
                    JumpStrength = -8.8f;
                    PipeSpacing = 200;
                    break;
            }
        }
    }
}
