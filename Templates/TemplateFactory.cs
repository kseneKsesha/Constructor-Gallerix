using System;
using System.Collections.Generic;
using System.Linq;

namespace Constructor_Gallerix.Templates
{
    public static class TemplateFactory
    {
        private static readonly Dictionary<string, Func<IExhibitionTemplate>> _templates =
            new Dictionary<string, Func<IExhibitionTemplate>>(StringComparer.OrdinalIgnoreCase)
            {
                ["ClassicGrid"] = () => new ClassicGridTemplate(),
                ["Carousel"] = () => new CarouselTemplate(),
                ["Mosaic"] = () => new MosaicTemplate(),
                ["FullscreenSlider"] = () => new FullscreenSliderTemplate()
            };

        public static IExhibitionTemplate CreateTemplate(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                return new ClassicGridTemplate();

            var templateKey = _templates.Keys.FirstOrDefault(k =>
                string.Equals(k, templateName, StringComparison.OrdinalIgnoreCase));

            if (templateKey != null && _templates.TryGetValue(templateKey, out var factory))
                return factory();

            return new ClassicGridTemplate();
        }

        public static IEnumerable<string> AvailableTemplates => _templates.Keys;

        public static bool IsValidTemplate(string templateName)
        {
            return _templates.Keys.Any(k =>
                string.Equals(k, templateName, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetDisplayName(string templateKey)
        {
            switch (templateKey)
            {
                case "ClassicGrid":
                    return "Классическая сетка";
                case "Carousel":
                    return "Карусель";
                case "Mosaic":
                    return "Мозаика";
                case "FullscreenSlider":
                    return "Полноэкранный слайдер";
                default:
                    return "Неизвестный шаблон";
            }
        }
    }
}