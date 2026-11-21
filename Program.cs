using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using FlappyBird.Core;
using FlappyBird.Enums;
using FlappyBird.Forms;

namespace FlappyBird
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--generate-screenshots")
            {
                GenerateScreenshots();
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }

        private static void GenerateScreenshots()
        {
            string screenshotsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "screenshots");
            Directory.CreateDirectory(screenshotsDir);

            int width = 420;
            int height = 600;

            // 1. Ekran Görüntüsü: Başlangıç Menüsü
            using (Bitmap bmp1 = new Bitmap(width, height))
            using (Graphics g1 = Graphics.FromImage(bmp1))
            using (GameEngine engine1 = new GameEngine(width, height))
            {
                engine1.Render(g1);
                bmp1.Save(Path.Combine(screenshotsDir, "menu.png"), ImageFormat.Png);
            }

            // 2. Ekran Görüntüsü: Oyun İçi (Gün Batımı Teması + Mavi Kuş + Altın Toplama)
            using (Bitmap bmp2 = new Bitmap(width, height))
            using (Graphics g2 = Graphics.FromImage(bmp2))
            using (GameEngine engine2 = new GameEngine(width, height))
            {
                engine2.CurrentTheme = GameTheme.Sunset; // Gün batımı
                engine2.Bird.Skin = BirdSkin.SkyBlue;     // Mavi Kuş
                engine2.Flap(); // Playing moduna geçir
                for (int i = 0; i < 40; i++)
                {
                    engine2.Update();
                }
                engine2.Bird.Y = 220;
                engine2.Bird.VelocityY = -2f;
                engine2.ScoreManager.IncrementScore();
                engine2.ScoreManager.IncrementScore();
                engine2.ScoreManager.IncrementScore();
                engine2.ScoreManager.IncrementScore();
                engine2.ScoreManager.IncrementScore(); // Skor: 5
                engine2.Pipes[0].X = 180;
                engine2.Pipes[0].TopHeight = 155;
                if (engine2.Coins.Count > 0)
                {
                    engine2.Coins[0].X = 180 + (engine2.Pipes[0].Width / 2f);
                    engine2.Coins[0].BaseY = 155 + (engine2.Pipes[0].Gap / 2f);
                    engine2.Coins[0].IsCollected = false;
                }
                engine2.Pipes[1].X = 405;

                engine2.Render(g2);
                bmp2.Save(Path.Combine(screenshotsDir, "gameplay.png"), ImageFormat.Png);
            }

            // 3. Ekran Görüntüsü: Gece Teması ve Oyun Bitti (Game Over)
            using (Bitmap bmp3 = new Bitmap(width, height))
            using (Graphics g3 = Graphics.FromImage(bmp3))
            using (GameEngine engine3 = new GameEngine(width, height))
            {
                engine3.CurrentTheme = GameTheme.Night; // Gece modu (Yıldızlar + Ay)
                engine3.Bird.Skin = BirdSkin.GoldenPhoenix; // Altın Anka Kuş
                engine3.Flap();
                for (int i = 0; i < 24; i++)
                {
                    engine3.ScoreManager.IncrementScore();
                }
                typeof(GameEngine).GetProperty("CollectedCoins")?.SetValue(engine3, 14);

                engine3.Bird.Y = height - engine3.Ground.Height - engine3.Bird.Height;
                engine3.Bird.IsAlive = false;

                typeof(GameEngine)
                    .GetProperty("State")?
                    .SetValue(engine3, GameState.GameOver);

                engine3.Render(g3);
                bmp3.Save(Path.Combine(screenshotsDir, "gameover.png"), ImageFormat.Png);
            }

            Console.WriteLine("Ekran görüntüleri başarıyla üretildi: " + screenshotsDir);
        }
    }
}
