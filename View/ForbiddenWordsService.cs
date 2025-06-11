// File: ForbiddenWordsService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Constructor_Gallerix.View
{
    /// <summary>
    /// Статический сервис, который:
    /// 1. Загружает запрещённые слова из текстового файла forbidden_words.txt
    /// 2. Позволяет добавлять/удалять слово из списка и сохранять изменения в файл
    /// 3. Проверяет, содержится ли в переданном тексте какое-либо запрещённое слово
    /// </summary>
    public static class ForbiddenWordsService
    {
        // Абсолютный путь к файлу forbidden_words.txt в папке, где лежит EXE
        private static readonly string filePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "forbidden_words.txt");

        // Внутренний список запрещённых слов (в нижнем регистре, без дубликатов)
        private static List<string> forbiddenWords = new List<string>();

        static ForbiddenWordsService()
        {
            LoadWords();
        }

        /// <summary>
        /// Считывает все строки из файла и сохраняет их в forbiddenWords.
        /// Если файла нет — создаёт пустой.
        /// </summary>
        private static void LoadWords()
        {
            forbiddenWords.Clear();

            if (!File.Exists(filePath))
            {
                // Создаём пустой файл, чтобы не вываливаться на последующих операциях
                File.WriteAllText(filePath, string.Empty);
                return;
            }

            foreach (var line in File.ReadAllLines(filePath))
            {
                var w = line.Trim().ToLower();
                if (!string.IsNullOrEmpty(w))
                    forbiddenWords.Add(w);
            }

            // Убираем дубликаты
            forbiddenWords = forbiddenWords.Distinct().ToList();
        }

        /// <summary>
        /// Добавляет слово в список и сразу сохраняет его в файл.
        /// </summary>
        public static void AddForbiddenWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return;
            var w = word.Trim().ToLower();
            if (!forbiddenWords.Contains(w))
            {
                forbiddenWords.Add(w);
                SaveWords();
            }
        }

        /// <summary>
        /// Удаляет слово из списка и сразу сохраняет изменения.
        /// </summary>
        public static void RemoveForbiddenWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return;
            var w = word.Trim().ToLower();
            if (forbiddenWords.Contains(w))
            {
                forbiddenWords.Remove(w);
                SaveWords();
            }
        }

        /// <summary>
        /// Сохраняет текущее содержимое forbiddenWords обратно в файл forbidden_words.txt.
        /// </summary>
        private static void SaveWords()
        {
            File.WriteAllLines(filePath, forbiddenWords.Distinct());
        }

        /// <summary>
        /// Проверяет, есть ли в переданном тексте хотя бы одно запрещённое слово.
        /// Возвращает true, если хоть одно слово из forbiddenWords содержится в text как подстрока.
        /// </summary>
        public static bool ContainsForbiddenWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var lower = text.ToLower();
            foreach (var w in forbiddenWords)
            {
                if (lower.Contains(w))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Перезагружает список из файла (вызывать после того, как файл изменился извне).
        /// </summary>
        public static void Reload()
        {
            LoadWords();
        }

        /// <summary>
        /// Отладочный метод: возвращает весь список запрещённых слов (чтобы, например, отобразить в UI).
        /// </summary>
        public static IEnumerable<string> GetAllForbidden()
        {
            return forbiddenWords.AsReadOnly();
        }
    }
}
