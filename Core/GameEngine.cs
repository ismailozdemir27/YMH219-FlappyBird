using System.Drawing;
using System.Drawing.Drawing2D;
using FlappyBird.Enums;
using FlappyBird.Models;
using FlappyBird.Services;

namespace FlappyBird.Core
{
    /// <summary>
    /// Oyunun tüm mantıksal döngüsünü, fiziğini, çarpışma kontrollerini ve
    /// durum yönetimini üstlenen çekirdek motor sınıfı.
    /// Nesne tabanlı mimarinin merkezi bileşenidir.
    /// </summary>
    public class GameEngine : IDisposable
    {
        public int ScreenWidth { get; }
        public int ScreenHeight { get; }

        public Bird Bird { get; private set; }
        public List<Pipe> Pipes { get; private set; }
        public Ground Ground { get; private set; }
        public ScoreManager ScoreManager { get; private set; }
        public SoundManager SoundManager { get; private set; }
        public GameSettings Settings { get; private set; }

        public GameState State { get; private set; }
        public float CurrentSpeed { get; private set; }
        public int Level { get; private set; } = 1;

        public event Action? OnGameOver;
        public event Action? OnScoreChanged;

        private readonly Random _random = new Random();
        private double _gameTimeSeconds = 0;
        private readonly float _birdStartX = 75;
        private readonly float _birdStartY = 195;

        // Arka plan bulutları için liste
        private readonly List<PointF> _clouds = new List<PointF>();

        public GameEngine(int screenWidth, int screenHeight)
        {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;

            Settings = new GameSettings();
            ScoreManager = new ScoreManager();
            SoundManager = new SoundManager();

            Bird = new Bird(_birdStartX, _birdStartY);
            Ground = new Ground();
            Pipes = new List<Pipe>();

            InitializeClouds();
            ResetGame();
        }

        private void InitializeClouds()
        {
            _clouds.Clear();
            _clouds.Add(new PointF(40, 80));
            _clouds.Add(new PointF(180, 140));
            _clouds.Add(new PointF(320, 60));
        }

        /// <summary>
        /// Oyunu sıfırlar ve bekleme (Ready) moduna alır.
        /// </summary>
        public void ResetGame()
        {
            State = GameState.Ready;
            CurrentSpeed = Settings.BasePipeSpeed;
            Level = 1;

            ScoreManager.ResetScore();
            Bird.Reset(_birdStartX, _birdStartY);
            Bird.Gravity = Settings.Gravity;
            Bird.JumpStrength = Settings.JumpStrength;

            Pipes.Clear();
            // Ekran üzerinde dönecek en az 3 engel oluştur
            float startPipeX = ScreenWidth + 80;
            for (int i = 0; i < 3; i++)
            {
                Pipes.Add(CreateNewPipe(startPipeX + (i * Settings.PipeSpacing)));
            }
        }

        /// <summary>
        /// Rastgele açıklık yüksekliğinde yeni bir boru çifti üretir.
        /// </summary>
        private Pipe CreateNewPipe(float x)
        {
            int groundY = ScreenHeight - Ground.Height;
            int minPipeHeight = 60;
            int maxTopHeight = groundY - Settings.PipeGap - minPipeHeight;

            if (maxTopHeight < minPipeHeight)
            {
                maxTopHeight = minPipeHeight + 40;
            }

            int topHeight = _random.Next(minPipeHeight, maxTopHeight);
            return new Pipe(x, topHeight, Settings.PipeGap);
        }

        /// <summary>
        /// Oyuncu zıplama eylemi (Space, Tıklama veya Yukarı Ok tuşu).
        /// </summary>
        public void Flap()
        {
            if (State == GameState.Ready)
            {
                State = GameState.Playing;
                Bird.Jump();
                SoundManager.PlayJump();
            }
            else if (State == GameState.Playing && Bird.IsAlive)
            {
                Bird.Jump();
                SoundManager.PlayJump();
            }
            else if (State == GameState.GameOver)
            {
                ResetGame();
            }
        }

        /// <summary>
        /// Oyunu duraklatır veya devam ettirir.
        /// </summary>
        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                State = GameState.Paused;
            }
            else if (State == GameState.Paused)
            {
                State = GameState.Playing;
            }
        }

        /// <summary>
        /// Zorluk derecesini değiştirir ve oyunu yeniden başlatır.
        /// </summary>
        public void ChangeDifficulty(Difficulty difficulty)
        {
            Settings.ApplyDifficulty(difficulty);
            ResetGame();
        }

        /// <summary>
        /// Kuş kostümünü (rengini) bir sonrakine geçirir.
        /// </summary>
        public void CycleBirdSkin()
        {
            var values = (BirdSkin[])Enum.GetValues(typeof(BirdSkin));
            int currentIndex = Array.IndexOf(values, Bird.Skin);
            int nextIndex = (currentIndex + 1) % values.Length;
            Bird.Skin = values[nextIndex];
            SoundManager.PlayScore();
        }

        /// <summary>
        /// Timer tarafından yaklaşık 60 FPS frekansla çağrılan ana oyun döngüsü metodu.
        /// </summary>
        public void Update()
        {
            _gameTimeSeconds += 0.016;

            // Bulutların yavaş süzülmesi
            for (int i = 0; i < _clouds.Count; i++)
            {
                float cx = _clouds[i].X - 0.4f;
                if (cx < -120) cx = ScreenWidth + 50;
                _clouds[i] = new PointF(cx, _clouds[i].Y);
            }

            if (State == GameState.Ready)
            {
                // Başlamadan önce kuş nazikçe havada salınır
                Bird.IdleFloat(_birdStartY, _gameTimeSeconds);
                Ground.Update(CurrentSpeed * 0.5f);
                return;
            }

            if (State == GameState.Paused)
            {
                return;
            }

            if (State == GameState.GameOver)
            {
                // Kuş yere düşene kadar yerçekimi devam eder
                int groundY = ScreenHeight - Ground.Height;
                if (Bird.Y + Bird.Height < groundY)
                {
                    Bird.Update(groundY);
                }
                return;
            }

            int groundTopY = ScreenHeight - Ground.Height;

            // Kuş fiziği güncelle
            Bird.Update(groundTopY);

            // Zemin kayması
            Ground.Update(CurrentSpeed);

            // Boruların hareketi ve yeniden konumlandırılması
            float maxRightX = 0;
            foreach (var p in Pipes)
            {
                if (p.X > maxRightX) maxRightX = p.X;
            }

            for (int i = 0; i < Pipes.Count; i++)
            {
                Pipe pipe = Pipes[i];
                pipe.Update(CurrentSpeed);

                // Skor kontrolü: Kuş borunun ortasını geçtiğinde skor ekle
                if (!pipe.Passed && pipe.X + (pipe.Width / 2) < Bird.X)
                {
                    pipe.Passed = true;
                    bool newBest = ScoreManager.IncrementScore();
                    SoundManager.PlayScore();
                    OnScoreChanged?.Invoke();

                    // Seviye ve hız artışı (Her 5 puanda bir hız %5 artar)
                    int calculatedLevel = (ScoreManager.CurrentScore / 5) + 1;
                    if (calculatedLevel != Level)
                    {
                        Level = calculatedLevel;
                        CurrentSpeed = Settings.BasePipeSpeed + ((Level - 1) * 0.35f);
                    }
                }

                // Ekrandan çıkan boruyu sıraya en sağdan tekrar dahil et (Recycle)
                if (pipe.X + pipe.Width < -10)
                {
                    float nextX = Math.Max(ScreenWidth + 30, maxRightX + Settings.PipeSpacing);
                    Pipes[i] = CreateNewPipe(nextX);
                    maxRightX = nextX;
                }
            }

            // Çarpışma Denetimi (Collision Detection)
            CheckCollisions();
        }

        /// <summary>
        /// Kuşun borulara veya zemine çarpmasını kontrol eder.
        /// </summary>
        private void CheckCollisions()
        {
            int groundY = ScreenHeight - Ground.Height;

            // 1. Zemin çarpışması
            if (Bird.Y + Bird.Height >= groundY)
            {
                TriggerGameOver();
                return;
            }

            // 2. Borular ile çarpışma
            Rectangle birdBox = Bird.Hitbox;
            foreach (var pipe in Pipes)
            {
                if (pipe.CollidesWith(birdBox, ScreenHeight, Ground.Height))
                {
                    TriggerGameOver();
                    return;
                }
            }
        }

        private void TriggerGameOver()
        {
            if (State != GameState.GameOver)
            {
                State = GameState.GameOver;
                Bird.IsAlive = false;
                SoundManager.PlayHit();
                SoundManager.PlayDie();
                OnGameOver?.Invoke();
            }
        }

        /// <summary>
        /// Tüm oyun ekranını GDI+ çift tamponlama ile çizer.
        /// </summary>
        public void Render(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;

            // 1. Gökyüzü arka planı (Gradient)
            DrawSky(g);

            // 2. Bulutlar
            DrawClouds(g);

            // 3. Borular (Engeller)
            foreach (var pipe in Pipes)
            {
                pipe.Draw(g, ScreenHeight, Ground.Height);
            }

            // 4. Zemin
            Ground.Draw(g, ScreenWidth, ScreenHeight);

            // 5. Kuş
            Bird.Draw(g);

            // 6. HUD / Arayüz Bilgileri
            DrawHUD(g);

            // 7. Durum Katmanları (Ready / Paused / Game Over)
            if (State == GameState.Ready)
            {
                DrawReadyOverlay(g);
            }
            else if (State == GameState.Paused)
            {
                DrawPausedOverlay(g);
            }
            else if (State == GameState.GameOver)
            {
                DrawGameOverOverlay(g);
            }
        }

        private void DrawSky(Graphics g)
        {
            using (LinearGradientBrush skyBrush = new LinearGradientBrush(
                new Rectangle(0, 0, ScreenWidth, ScreenHeight - Ground.Height),
                Color.FromArgb(112, 197, 206), // Flappy retro gökyüzü mavisi
                Color.FromArgb(178, 230, 236),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(skyBrush, 0, 0, ScreenWidth, ScreenHeight);
            }

            // Ufuk çizgisi silüeti (Şehir binaları)
            int cityY = ScreenHeight - Ground.Height - 45;
            using (SolidBrush cityBrush = new SolidBrush(Color.FromArgb(140, 185, 218, 200)))
            {
                for (int x = 0; x < ScreenWidth; x += 40)
                {
                    int h = 25 + ((x * 17) % 25);
                    g.FillRectangle(cityBrush, x, cityY + (45 - h), 36, h);
                }
            }
        }

        private void DrawClouds(Graphics g)
        {
            using (SolidBrush cloudBrush = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
            {
                foreach (var c in _clouds)
                {
                    g.FillEllipse(cloudBrush, c.X, c.Y, 70, 32);
                    g.FillEllipse(cloudBrush, c.X + 20, c.Y - 14, 45, 36);
                    g.FillEllipse(cloudBrush, c.X + 45, c.Y, 50, 28);
                }
            }
        }

        private void DrawHUD(Graphics g)
        {
            // Oyun esnasında büyük retro skor sayısı
            if (State == GameState.Playing)
            {
                string scoreText = ScoreManager.CurrentScore.ToString();
                using (Font font = new Font("Impact", 36, FontStyle.Bold))
                {
                    SizeF size = g.MeasureString(scoreText, font);
                    float x = (ScreenWidth - size.Width) / 2f;
                    float y = 45;

                    // Kalın siyah kontur
                    using (FontFamily ff = new FontFamily("Impact"))
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        path.AddString(scoreText, ff, (int)FontStyle.Bold, 48, new PointF(x, y), StringFormat.GenericDefault);
                        using (Pen outlinePen = new Pen(Color.FromArgb(20, 20, 20), 5))
                        {
                            g.DrawPath(outlinePen, path);
                        }
                        g.FillPath(Brushes.White, path);
                    }
                }
            }

            // Üst bar mini bilgi (Zorluk Seviyesi ve Hız)
            string diffText = $"Mod: {Settings.CurrentDifficulty} | Hız: {CurrentSpeed:F1}x";
            using (Font miniFont = new Font("Segoe UI", 9, FontStyle.Bold))
            {
                g.DrawString(diffText, miniFont, Brushes.White, 12, 10);
            }

            // Ses durumu
            string soundText = SoundManager.SoundEnabled ? "Ses: Açık (M)" : "Ses: Kapalı (M)";
            using (Font miniFont = new Font("Segoe UI", 9, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(soundText, miniFont);
                g.DrawString(soundText, miniFont, Brushes.White, ScreenWidth - size.Width - 12, 10);
            }
        }

        private void DrawReadyOverlay(Graphics g)
        {
            int centerX = ScreenWidth / 2;

            // Başlık
            DrawOutlinedText(g, "FLAPPY BIRD", centerX, 85, 36, Color.Gold, Color.Black);

            // Yönerge kutusu
            int boxW = 290;
            int boxH = 138;
            int boxX = centerX - (boxW / 2);
            int boxY = 245;

            using (SolidBrush bannerBrush = new SolidBrush(Color.FromArgb(185, 20, 30, 40)))
            {
                g.FillRoundRectangle(bannerBrush, new Rectangle(boxX, boxY, boxW, boxH), 16);
            }
            using (Pen boxPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
            {
                g.DrawRoundRectangle(boxPen, new Rectangle(boxX, boxY, boxW, boxH), 16);
            }

            using (Font textFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            {
                DrawCenteredText(g, "Uçmak için SPACE'e Basın!", textFont, Brushes.White, centerX, boxY + 14);
                DrawCenteredText(g, $"< C: Kostüm: {Bird.Skin.GetDisplayName()} >", textFont, Brushes.LightGreen, centerX, boxY + 42);
                DrawCenteredText(g, "1: Kolay | 2: Orta | 3: Zor", textFont, Brushes.Yellow, centerX, boxY + 72);
                DrawCenteredText(g, "P: Duraklat | M: Ses Aç/Kapa", textFont, Brushes.LightSkyBlue, centerX, boxY + 102);
            }

            // En yüksek skor bilgisi
            string bestText = $"En Yüksek Skor: {ScoreManager.HighScore}";
            DrawOutlinedText(g, bestText, centerX, 405, 16, Color.White, Color.Black);
        }

        private void DrawPausedOverlay(Graphics g)
        {
            using (SolidBrush dimBrush = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillRectangle(dimBrush, 0, 0, ScreenWidth, ScreenHeight);
            }

            DrawOutlinedText(g, "DURAKLATILDI", ScreenWidth / 2, 230, 28, Color.Yellow, Color.Black);
            using (Font subFont = new Font("Segoe UI", 12, FontStyle.Bold))
            {
                DrawCenteredText(g, "Devam etmek için P veya ESC'ye basın", subFont, Brushes.White, ScreenWidth / 2, 280);
            }
        }

        private void DrawGameOverOverlay(Graphics g)
        {
            int centerX = ScreenWidth / 2;

            // Game Over başlığı
            DrawOutlinedText(g, "OYUN BİTTİ", centerX, 95, 34, Color.Crimson, Color.Black);

            // Skor tablosu paneli
            int panelW = 270;
            int panelH = 175;
            int panelX = centerX - (panelW / 2);
            int panelY = 150;

            using (SolidBrush panelBg = new SolidBrush(Color.FromArgb(238, 226, 180)))
            {
                g.FillRoundRectangle(panelBg, new Rectangle(panelX, panelY, panelW, panelH), 14);
            }
            using (Pen borderPen = new Pen(Color.FromArgb(120, 80, 40), 3))
            {
                g.DrawRoundRectangle(borderPen, new Rectangle(panelX, panelY, panelW, panelH), 14);
            }

            // Madalya alanı
            int medalX = panelX + 22;
            int medalY = panelY + 45;
            DrawMedal(g, medalX, medalY, ScoreManager.CurrentScore);

            // Skorlar metinleri
            using (Font labelFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (Font valFont = new Font("Impact", 20))
            {
                g.DrawString("SKOR", labelFont, Brushes.DarkOrange, panelX + 115, panelY + 22);
                g.DrawString(ScoreManager.CurrentScore.ToString(), valFont, Brushes.Black, panelX + 115, panelY + 40);

                g.DrawString("EN YÜKSEK", labelFont, Brushes.DarkOrange, panelX + 115, panelY + 75);
                g.DrawString(ScoreManager.HighScore.ToString(), valFont, Brushes.Black, panelX + 115, panelY + 93);
            }

            // Yeni rekor rozeti
            if (ScoreManager.IsNewHighScore)
            {
                using (SolidBrush newBadge = new SolidBrush(Color.FromArgb(220, 30, 30)))
                {
                    g.FillRoundRectangle(newBadge, new Rectangle(panelX + panelW - 62, panelY + 14, 48, 20), 6);
                }
                using (Font badgeFont = new Font("Segoe UI", 8, FontStyle.Bold))
                {
                    g.DrawString("YENİ!", badgeFont, Brushes.White, panelX + panelW - 55, panelY + 17);
                }
            }

            // Seviye bilgisi
            using (Font infoFont = new Font("Segoe UI", 9, FontStyle.Italic))
            {
                g.DrawString($"Ulaşılan Seviye: {Level}", infoFont, Brushes.SaddleBrown, panelX + 115, panelY + 135);
            }

            // Tekrar başlatma talimatı
            using (Font retryFont = new Font("Segoe UI", 12, FontStyle.Bold))
            {
                DrawCenteredText(g, "Yeniden Başlatmak İçin SPACE veya R", retryFont, Brushes.White, centerX, panelY + panelH + 30);
            }
        }

        private void DrawMedal(Graphics g, int x, int y, int score)
        {
            Color medalColor = Color.DarkGray;
            string medalName = "Yok";

            if (score >= 30)
            {
                medalColor = Color.FromArgb(229, 228, 226); // Platin
                medalName = "Platin";
            }
            else if (score >= 20)
            {
                medalColor = Color.FromArgb(255, 215, 0); // Altın
                medalName = "Altın";
            }
            else if (score >= 10)
            {
                medalColor = Color.FromArgb(192, 192, 192); // Gümüş
                medalName = "Gümüş";
            }
            else if (score >= 5)
            {
                medalColor = Color.FromArgb(205, 127, 50); // Bronz
                medalName = "Bronz";
            }

            int size = 52;
            using (SolidBrush brush = new SolidBrush(medalColor))
            {
                g.FillEllipse(brush, x, y, size, size);
            }
            using (Pen pen = new Pen(Color.FromArgb(80, 50, 20), 2.5f))
            {
                g.DrawEllipse(pen, x, y, size, size);
                g.DrawEllipse(pen, x + 5, y + 5, size - 10, size - 10);
            }

            using (Font font = new Font("Segoe UI", 7, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(medalName, font);
                g.DrawString(medalName, font, Brushes.Black, x + (size - sz.Width) / 2f, y + (size - sz.Height) / 2f);
            }
        }

        private static void DrawOutlinedText(Graphics g, string text, int centerX, int y, int size, Color fill, Color outline)
        {
            using (FontFamily ff = new FontFamily("Impact"))
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddString(text, ff, (int)FontStyle.Bold, size, new PointF(0, 0), StringFormat.GenericDefault);
                RectangleF bounds = path.GetBounds();
                float x = centerX - (bounds.Width / 2f);

                GraphicsState state = g.Save();
                g.TranslateTransform(x - bounds.Left, y);
                using (Pen pen = new Pen(outline, 4.5f))
                {
                    pen.LineJoin = LineJoin.Round;
                    g.DrawPath(pen, path);
                }
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
                g.Restore(state);
            }
        }

        private static void DrawCenteredText(Graphics g, string text, Font font, Brush brush, int centerX, int y)
        {
            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, brush, centerX - (size.Width / 2f), y);
        }

        public void Dispose()
        {
            SoundManager?.Dispose();
        }
    }

    /// <summary>
    /// GDI+ için yuvarlatılmış dikdörtgen çizim eklentisi.
    /// </summary>
    public static class GraphicsExtensions
    {
        public static void FillRoundRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using GraphicsPath path = CreateRoundedRectPath(rect, radius);
            g.FillPath(brush, path);
        }

        public static void DrawRoundRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using GraphicsPath path = CreateRoundedRectPath(rect, radius);
            g.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
