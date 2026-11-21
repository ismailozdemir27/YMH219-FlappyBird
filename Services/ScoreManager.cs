using System.IO;

namespace FlappyBird.Services
{
    /// <summary>
    /// Skor ve en yüksek skor (High Score) kayıt işlemlerini yöneten servis sınıfı.
    /// Skor bilgilerini yerel dosyada (highscore.txt) saklar ve yükler.
    /// </summary>
    public class ScoreManager
    {
        public int CurrentScore { get; private set; }
        public int HighScore { get; private set; }
        public bool IsNewHighScore { get; private set; }

        private readonly string _scoreFilePath;

        public ScoreManager()
        {
            CurrentScore = 0;
            _scoreFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "highscore.txt");
            LoadHighScore();
        }

        /// <summary>
        /// Skoru bir puan artırır ve gerekiyorsa en yüksek skoru günceller.
        /// </summary>
        public bool IncrementScore()
        {
            CurrentScore++;

            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
                IsNewHighScore = true;
                SaveHighScore();
                return true; // Yeni rekor kırıldı
            }

            return false;
        }

        /// <summary>
        /// Yeni oyun için skoru sıfırlar.
        /// </summary>
        public void ResetScore()
        {
            CurrentScore = 0;
            IsNewHighScore = false;
        }

        /// <summary>
        /// En yüksek skoru dosyadan yükler. Dosya yoksa veya bozuksa 0 kabul eder.
        /// </summary>
        public void LoadHighScore()
        {
            try
            {
                if (File.Exists(_scoreFilePath))
                {
                    string content = File.ReadAllText(_scoreFilePath).Trim();
                    if (int.TryParse(content, out int score))
                    {
                        HighScore = Math.Max(0, score);
                        return;
                    }
                }
            }
            catch
            {
                // Dosya okuma hatası olursa 0 ile devam edilir
            }

            HighScore = 0;
        }

        /// <summary>
        /// En yüksek skoru dosyaya kaydeder.
        /// </summary>
        public void SaveHighScore()
        {
            try
            {
                File.WriteAllText(_scoreFilePath, HighScore.ToString());
            }
            catch
            {
                // Hata oluşsa bile oyunun çökmesi engellenir
            }
        }
    }
}
