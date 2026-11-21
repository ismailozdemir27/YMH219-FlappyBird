using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using FlappyBird.Enums;

namespace FlappyBird.Models
{
    /// <summary>
    /// Oyuncunun kontrol ettiği kuş karakterini temsil eden sınıf.
    /// Nesne tabanlı programlama ilkelerine uygun olarak fizik kuralları, görünüm (skin) ve çizim metodlarını barındırır.
    /// </summary>
    public class Bird
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int Width { get; } = 36;
        public int Height { get; } = 28;

        public float VelocityY { get; set; }
        public float Gravity { get; set; } = 0.42f;
        public float JumpStrength { get; set; } = -8.2f;

        public bool IsAlive { get; set; } = true;
        public BirdSkin Skin { get; set; } = BirdSkin.ClassicYellow;

        private float _rotation = 0f;
        private int _wingFrame = 0;
        private int _wingTick = 0;

        public Bird(float startX, float startY)
        {
            Reset(startX, startY);
        }

        /// <summary>
        /// Kuşu başlangıç konumuna ve durumuna sıfırlar.
        /// </summary>
        public void Reset(float startX, float startY)
        {
            X = startX;
            Y = startY;
            VelocityY = 0;
            IsAlive = true;
            _rotation = 0;
            _wingFrame = 0;
            _wingTick = 0;
        }

        /// <summary>
        /// Kuşun yukarı doğru sıçramasını sağlar.
        /// </summary>
        public void Jump()
        {
            if (!IsAlive) return;

            VelocityY = JumpStrength;
            _rotation = -25f; // Zıplama anında burnunu yukarı diker
        }

        /// <summary>
        /// Kuşun fiziksel hareketini günceller.
        /// </summary>
        public void Update(int groundY)
        {
            // Yer çekimi etkisi
            VelocityY += Gravity;

            // Terminal velocity sınırlaması
            if (VelocityY > 11f)
            {
                VelocityY = 11f;
            }

            Y += VelocityY;

            // Dönüş açısını hızına göre güncelle
            if (VelocityY > 0)
            {
                _rotation += 3.2f;
                if (_rotation > 70f)
                {
                    _rotation = 70f;
                }
            }
            else
            {
                _rotation = -22f;
            }

            // Kanat çırpma animasyonu
            if (IsAlive)
            {
                _wingTick++;
                if (_wingTick % 5 == 0)
                {
                    _wingFrame = (_wingFrame + 1) % 3;
                }
            }

            // Tavan ve zemin sınırları
            if (Y < 0)
            {
                Y = 0;
                VelocityY = 0;
            }

            if (Y + Height >= groundY)
            {
                Y = groundY - Height;
                VelocityY = 0;
                IsAlive = false;
            }
        }

        /// <summary>
        /// Bekleme modunda tatlı bir süzülme animasyonu yapar.
        /// </summary>
        public void IdleFloat(float baseY, double timeSeconds)
        {
            Y = baseY + (float)(Math.Sin(timeSeconds * 4.5) * 6.0);
            _rotation = (float)(Math.Sin(timeSeconds * 4.5) * 4.0);
            _wingTick++;
            if (_wingTick % 6 == 0)
            {
                _wingFrame = (_wingFrame + 1) % 3;
            }
        }

        /// <summary>
        /// Kuşun çarpışma kutusu (Hitbox).
        /// Oyuncu lehine 3 piksellik kenar payı bırakılarak adil bir çarpışma sağlanır.
        /// </summary>
        public Rectangle Hitbox
        {
            get
            {
                int padding = 3;
                return new Rectangle(
                    (int)X + padding,
                    (int)Y + padding,
                    Width - (padding * 2),
                    Height - (padding * 2)
                );
            }
        }

        /// <summary>
        /// Kuşu GDI+ ile seçili kostüme (Skin) göre estetik ve vektörel şekilde çizer.
        /// </summary>
        public void Draw(Graphics g)
        {
            GraphicsState state = g.Save();

            float centerX = X + Width / 2f;
            float centerY = Y + Height / 2f;

            // Kuşun merkezine göre döndür
            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(_rotation);
            g.TranslateTransform(-centerX, -centerY);

            // Yumuşak kenar
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Seçili kostümün renk paletini belirle
            GetSkinColors(out Color bodyTop, out Color bodyBottom, out Color bellyColor,
                          out Color wingColor, out Color beakColor, out Color borderColor, out Color wingBorder);

            // 1. Gövde
            using (GraphicsPath bodyPath = new GraphicsPath())
            {
                Rectangle bodyRect = new Rectangle((int)X, (int)Y, Width - 6, Height);
                bodyPath.AddEllipse(bodyRect);

                using (LinearGradientBrush bodyBrush = new LinearGradientBrush(
                    bodyRect,
                    bodyTop,
                    bodyBottom,
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(bodyBrush, bodyPath);
                }

                using (Pen borderPen = new Pen(borderColor, 2f))
                {
                    g.DrawPath(borderPen, bodyPath);
                }
            }

            // 2. Karın bölgesi
            using (GraphicsPath bellyPath = new GraphicsPath())
            {
                Rectangle bellyRect = new Rectangle((int)X + 4, (int)Y + Height - 12, Width - 18, 10);
                bellyPath.AddEllipse(bellyRect);
                using (SolidBrush bellyBrush = new SolidBrush(bellyColor))
                {
                    g.FillPath(bellyBrush, bellyPath);
                }
            }

            // 3. Kanat (Animasyonlu)
            int wingOffsetY = _wingFrame == 0 ? 0 : (_wingFrame == 1 ? -2 : 3);
            Rectangle wingRect = new Rectangle((int)X + 5, (int)Y + 9 + wingOffsetY, 14, 10);
            using (GraphicsPath wingPath = new GraphicsPath())
            {
                wingPath.AddEllipse(wingRect);
                using (SolidBrush wingBrush = new SolidBrush(wingColor))
                {
                    g.FillPath(wingBrush, wingPath);
                }
                using (Pen wingPen = new Pen(wingBorder, 1.6f))
                {
                    g.DrawPath(wingPen, wingPath);
                }
            }

            // 4. Göz (Büyük beyaz küre + siyah gözbebeği + ışıltı)
            int eyeX = (int)X + Width - 15;
            int eyeY = (int)Y + 4;
            int eyeSize = 10;
            g.FillEllipse(Brushes.White, eyeX, eyeY, eyeSize, eyeSize);
            using (Pen eyePen = new Pen(borderColor, 1.5f))
            {
                g.DrawEllipse(eyePen, eyeX, eyeY, eyeSize, eyeSize);
            }
            // Gözbebeği
            g.FillEllipse(Brushes.Black, eyeX + 4, eyeY + 3, 4, 4);
            // Işıltı
            g.FillEllipse(Brushes.White, eyeX + 5, eyeY + 4, 1, 1);

            // 5. Gaga
            Point[] beakPoints = new Point[]
            {
                new Point((int)X + Width - 8, (int)Y + 12),
                new Point((int)X + Width + 5, (int)Y + 17),
                new Point((int)X + Width - 8, (int)Y + 22)
            };
            using (SolidBrush beakBrush = new SolidBrush(beakColor))
            {
                g.FillPolygon(beakBrush, beakPoints);
            }
            using (Pen beakPen = new Pen(borderColor, 1.6f))
            {
                g.DrawPolygon(beakPen, beakPoints);
            }

            // 6. Yanak pembeliği
            using (SolidBrush cheekBrush = new SolidBrush(Color.FromArgb(100, 255, 120, 140)))
            {
                g.FillEllipse(cheekBrush, (int)X + Width - 17, (int)Y + 16, 7, 5);
            }

            // 7. Altın Anka için özel parıltı tacı
            if (Skin == BirdSkin.GoldenPhoenix)
            {
                using (SolidBrush crownBrush = new SolidBrush(Color.FromArgb(255, 235, 30)))
                using (Pen crownPen = new Pen(Color.FromArgb(150, 80, 0), 1.2f))
                {
                    Point[] crownPoints = new Point[]
                    {
                        new Point((int)X + 11, (int)Y),
                        new Point((int)X + 8, (int)Y - 6),
                        new Point((int)X + 13, (int)Y - 4),
                        new Point((int)X + 18, (int)Y - 8),
                        new Point((int)X + 22, (int)Y - 3),
                        new Point((int)X + 24, (int)Y)
                    };
                    g.FillPolygon(crownBrush, crownPoints);
                    g.DrawPolygon(crownPen, crownPoints);
                }
            }

            g.Restore(state);
        }

        private void GetSkinColors(
            out Color bodyTop, out Color bodyBottom, out Color bellyColor,
            out Color wingColor, out Color beakColor, out Color borderColor, out Color wingBorder)
        {
            switch (Skin)
            {
                case BirdSkin.SkyBlue:
                    bodyTop = Color.FromArgb(100, 200, 255);
                    bodyBottom = Color.FromArgb(30, 135, 240);
                    bellyColor = Color.FromArgb(225, 248, 255);
                    wingColor = Color.FromArgb(240, 252, 255);
                    beakColor = Color.FromArgb(255, 140, 20);
                    borderColor = Color.FromArgb(15, 55, 105);
                    wingBorder = Color.FromArgb(15, 55, 105);
                    break;

                case BirdSkin.RubyRed:
                    bodyTop = Color.FromArgb(255, 85, 85);
                    bodyBottom = Color.FromArgb(195, 25, 30);
                    bellyColor = Color.FromArgb(255, 225, 225);
                    wingColor = Color.FromArgb(255, 242, 242);
                    beakColor = Color.FromArgb(255, 205, 30);
                    borderColor = Color.FromArgb(95, 20, 20);
                    wingBorder = Color.FromArgb(95, 20, 20);
                    break;

                case BirdSkin.RosePink:
                    bodyTop = Color.FromArgb(255, 145, 200);
                    bodyBottom = Color.FromArgb(230, 85, 150);
                    bellyColor = Color.FromArgb(255, 235, 245);
                    wingColor = Color.FromArgb(255, 250, 253);
                    beakColor = Color.FromArgb(255, 150, 30);
                    borderColor = Color.FromArgb(95, 25, 55);
                    wingBorder = Color.FromArgb(95, 25, 55);
                    break;

                case BirdSkin.GoldenPhoenix:
                    bodyTop = Color.FromArgb(255, 235, 75);
                    bodyBottom = Color.FromArgb(235, 160, 0);
                    bellyColor = Color.FromArgb(255, 250, 195);
                    wingColor = Color.FromArgb(255, 245, 145);
                    beakColor = Color.FromArgb(255, 80, 0);
                    borderColor = Color.FromArgb(105, 60, 0);
                    wingBorder = Color.FromArgb(105, 60, 0);
                    break;

                default: // ClassicYellow
                    bodyTop = Color.FromArgb(255, 230, 40);
                    bodyBottom = Color.FromArgb(235, 170, 20);
                    bellyColor = Color.FromArgb(255, 255, 220);
                    wingColor = Color.FromArgb(255, 250, 240);
                    beakColor = Color.FromArgb(245, 95, 20);
                    borderColor = Color.FromArgb(90, 50, 10);
                    wingBorder = Color.FromArgb(90, 50, 10);
                    break;
            }
        }
    }
}
