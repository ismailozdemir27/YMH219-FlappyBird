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

            // 2. Ekran Görüntüsü: Oyun İçi (Gameplay)
            using (Bitmap bmp2 = new Bitmap(width, height))
            using (Graphics g2 = Graphics.FromImage(bmp2))
            using (GameEngine engine2 = new GameEngine(width, height))
            {
                engine2.Flap(); // Playing moduna geçir
                // Birkaç simülasyon adımı işlet
                for (int i = 0; i < 40; i++)
                {
                    engine2.Update();
                }
                // Kuşu ve boruyu güzel bir pozisyona ayarla
                engine2.Bird.Y = 230;
                engine2.Bird.VelocityY = -2f;
                engine2.ScoreManager.IncrementScore();
                engine2.ScoreManager.IncrementScore();
                engine2.ScoreManager.IncrementScore();
                engine2.ScoreManager.IncrementScore();
                engine2.ScoreManager.IncrementScore(); // Skor: 5
                engine2.Pipes[0].X = 180;
                engine2.Pipes[0].TopHeight = 170;
                engine2.Pipes[1].X = 400;

                engine2.Render(g2);
                bmp2.Save(Path.Combine(screenshotsDir, "gameplay.png"), ImageFormat.Png);
            }

            // 3. Ekran Görüntüsü: Oyun Bitti (Game Over)
            using (Bitmap bmp3 = new Bitmap(width, height))
            using (Graphics g3 = Graphics.FromImage(bmp3))
            using (GameEngine engine3 = new GameEngine(width, height))
            {
                engine3.Flap();
                for (int i = 0; i < 22; i++)
                {
                    engine3.ScoreManager.IncrementScore();
                }
                // Kuş zemine çarpmış pozisyon
                engine3.Bird.Y = height - engine3.Ground.Height - engine3.Bird.Height;
                engine3.Bird.IsAlive = false;

                // Game Over durumunu tetikle
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
