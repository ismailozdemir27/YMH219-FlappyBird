using FlappyBird.Enums;

namespace FlappyBird.Core
{
    /// <summary>
    /// Oyun ayarlarını ve zorluk seviyesi parametrelerini yöneten sınıf.
    /// Daha geniş boru boşlukları ve yumuşak süzülme fiziği ile dengelenmiştir.
    /// </summary>
    public class GameSettings
    {
        public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Medium;

        public int PipeGap { get; private set; } = 168;
        public float BasePipeSpeed { get; private set; } = 3.5f;
        public float Gravity { get; private set; } = 0.35f;
        public float JumpStrength { get; private set; } = -7.4f;
        public int PipeSpacing { get; private set; } = 225;

        public GameSettings()
        {
            ApplyDifficulty(Difficulty.Medium);
        }

        /// <summary>
        /// Seçilen zorluk seviyesine göre fiziksel oyun dinamiklerini ayarlar.
        /// Boru aralıkları ferah ve toplanabilir altınlar için rahat geçiş sağlayacak şekilde optimize edilmiştir.
        /// </summary>
        public void ApplyDifficulty(Difficulty difficulty)
        {
            CurrentDifficulty = difficulty;

            switch (difficulty)
            {
                case Difficulty.Easy:
                    PipeGap = 195;
                    BasePipeSpeed = 2.7f;
                    Gravity = 0.31f;
                    JumpStrength = -6.8f;
                    PipeSpacing = 245;
                    break;

                case Difficulty.Medium:
                    PipeGap = 168;
                    BasePipeSpeed = 3.5f;
                    Gravity = 0.35f;
                    JumpStrength = -7.4f;
                    PipeSpacing = 225;
                    break;

                case Difficulty.Hard:
                    PipeGap = 142;
                    BasePipeSpeed = 4.2f;
                    Gravity = 0.40f;
                    JumpStrength = -8.0f;
                    PipeSpacing = 205;
                    break;
            }
        }
    }
}
