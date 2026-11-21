using System.Drawing;
using System.Drawing.Drawing2D;

namespace FlappyBird.Models
{
    /// <summary>
    /// Zemin bileşenini temsil eden sınıf.
    /// Sürekli sola kayan zemin dokusunu ve zemin çarpışma sınırlarını yönetir.
    /// </summary>
    public class Ground
    {
        public int Height { get; } = 80;
        public float ScrollOffset { get; private set; } = 0;

        private readonly Color _grassTop = Color.FromArgb(115, 191, 46);
        private readonly Color _grassDark = Color.FromArgb(87, 155, 33);
        private readonly Color _dirtMain = Color.FromArgb(222, 215, 152);
        private readonly Color _dirtDark = Color.FromArgb(200, 191, 131);
        private readonly Color _borderLine = Color.FromArgb(60, 45, 10);

        /// <summary>
        /// Zemin dokusunu oyun hızına göre sola kaydırır.
        /// </summary>
        public void Update(float speed)
        {
            ScrollOffset += speed;
            if (ScrollOffset >= 24) // 24 piksel desen periyodu
            {
                ScrollOffset %= 24;
            }
        }

        /// <summary>
        /// Zemin çizimini gerçekleştirir.
        /// </summary>
        public void Draw(Graphics g, int screenWidth, int screenHeight)
        {
            int groundY = screenHeight - Height;

            // 1. Ana toprak arka planı
            using (SolidBrush dirtBrush = new SolidBrush(_dirtMain))
            {
                g.FillRectangle(dirtBrush, 0, groundY, screenWidth, Height);
            }

            // 2. Çim üst şerit
            using (SolidBrush grassBrush = new SolidBrush(_grassTop))
            {
                g.FillRectangle(grassBrush, 0, groundY, screenWidth, 14);
            }

            // 3. Çim testere dişi / desenler
            using (SolidBrush grassDarkBrush = new SolidBrush(_grassDark))
            {
                for (float x = -ScrollOffset; x < screenWidth + 24; x += 12)
                {
                    PointF[] points = new PointF[]
                    {
                        new PointF(x, groundY + 14),
                        new PointF(x + 6, groundY + 19),
                        new PointF(x + 12, groundY + 14)
                    };
                    g.FillPolygon(grassDarkBrush, points);
                }
            }

            // 4. Toprak dokusu eğik çizgileri (Haraket hissi veren klasik Flappy Bird çizgileri)
            using (Pen stripePen = new Pen(_dirtDark, 3f))
            {
                for (float x = -ScrollOffset - 24; x < screenWidth + 48; x += 24)
                {
                    g.DrawLine(stripePen, x, groundY + 25, x + 14, groundY + Height);
                }
            }

            // 5. Üst sınır çizgisi
            using (Pen borderPen = new Pen(_borderLine, 2f))
            {
                g.DrawLine(borderPen, 0, groundY, screenWidth, groundY);
            }
        }
    }
}
