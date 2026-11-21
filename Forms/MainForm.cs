using System;
using System.Drawing;
using System.Windows.Forms;
using FlappyBird.Core;
using FlappyBird.Enums;

namespace FlappyBird.Forms
{
    public partial class MainForm : Form
    {
        private readonly GameEngine _engine;

        public MainForm()
        {
            InitializeComponent();

            // Titremesiz akıcı 60 FPS grafik için çift tamponlama (Double Buffering)
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            UpdateStyles();

            DoubleBuffered = true;

            // Motoru form boyutuna göre başlat
            _engine = new GameEngine(ClientSize.Width, ClientSize.Height);

            // Form olayları
            Paint += MainForm_Paint;
            KeyDown += MainForm_KeyDown;
            MouseDown += MainForm_MouseDown;
            FormClosing += MainForm_FormClosing;

            // Zamanlayıcıyı başlat
            gameTimer.Start();
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            _engine.Update();
            Invalidate(); // Formu yeniden çizdir
        }

        private void MainForm_Paint(object? sender, PaintEventArgs e)
        {
            _engine.Render(e.Graphics);
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                case Keys.Up:
                    _engine.Flap();
                    e.Handled = true;
                    break;

                case Keys.P:
                case Keys.Escape:
                    _engine.TogglePause();
                    e.Handled = true;
                    break;

                case Keys.R:
                    _engine.ResetGame();
                    e.Handled = true;
                    break;

                case Keys.C:
                case Keys.Tab:
                    _engine.CycleBirdSkin();
                    e.Handled = true;
                    break;

                case Keys.T:
                    _engine.CycleTheme();
                    e.Handled = true;
                    break;

                case Keys.M:
                    _engine.SoundManager.SoundEnabled = !_engine.SoundManager.SoundEnabled;
                    e.Handled = true;
                    break;

                case Keys.D1:
                case Keys.NumPad1:
                    _engine.ChangeDifficulty(Difficulty.Easy);
                    e.Handled = true;
                    break;

                case Keys.D2:
                case Keys.NumPad2:
                    _engine.ChangeDifficulty(Difficulty.Medium);
                    e.Handled = true;
                    break;

                case Keys.D3:
                case Keys.NumPad3:
                    _engine.ChangeDifficulty(Difficulty.Hard);
                    e.Handled = true;
                    break;
            }
        }

        private void MainForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _engine.Flap();
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            gameTimer.Stop();
            _engine.Dispose();
        }
    }
}
