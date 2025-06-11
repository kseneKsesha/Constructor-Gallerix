using System;
using System.Speech.Synthesis;

namespace Constructor_Gallerix.View
{
    /// <summary>
    /// Сервис для озвучивания текстовых сообщений (аудиогид).
    /// </summary>
    internal static class AudioGuideService
    {
        private static readonly SpeechSynthesizer _synth;

        /// <summary>
        /// Подписка на завершение любой асинхронной речи.
        /// </summary>
        public static event EventHandler<SpeakCompletedEventArgs> SpeakCompleted;

        static AudioGuideService()
        {
            _synth = new SpeechSynthesizer
            {
                Volume = 100,
                Rate = 2  // увеличенная скорость речи
            };

            try
            {
                _synth.SelectVoice("Microsoft Irina");
            }
            catch
            {
                // Если нужный голос не найден, движок сам выберет дефолтный.
            }

            _synth.SetOutputToDefaultAudioDevice();
            _synth.SpeakCompleted += (s, e) => SpeakCompleted?.Invoke(s, e);

            // «Разогреваем» движок пустой фразой, чтобы при первом реальном вызове не было задержки:
            _synth.SpeakAsync(string.Empty);
        }

        /// <summary>
        /// Асинхронно произносит текст. Предыдущее воспроизведение прерывается.
        /// </summary>
        public static void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            _synth.SpeakAsyncCancelAll();
            _synth.SpeakAsync(text);
        }

        /// <summary>
        /// Останавливает текущее озвучивание, если оно идёт.
        /// </summary>
        public static void Stop()
        {
            _synth.SpeakAsyncCancelAll();
        }
    }
}