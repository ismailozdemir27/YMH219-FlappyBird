namespace FlappyBird.Enums
{
    /// <summary>
    /// Oyunun anlık durumunu belirten numaralandırma.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Oyun henüz başlamadı, oyuncunun zıplama tuşuna basması bekleniyor.
        /// </summary>
        Ready,

        /// <summary>
        /// Oyun aktif olarak oynanıyor.
        /// </summary>
        Playing,

        /// <summary>
        /// Oyun duraklatıldı.
        /// </summary>
        Paused,

        /// <summary>
        /// Kuş engele veya zemine çarptı, oyun bitti.
        /// </summary>
        GameOver
    }
}
