using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using Constructor_Gallerix.Models;

namespace Constructor_Gallerix.Templates
{
    public interface IExhibitionTemplate
    {
        UIElement Render(
            List<Picture> pictures,
            string title,
            string description,
            BitmapImage coverImage,
            RoutedEventHandler pictureClickHandler = null);

        bool IsSpecialMode { get; }
    }

    public static class ExhibitionTemplateExtensions
    {
        public static UIElement RenderExhibition(
            this IExhibitionTemplate template,
            Exhibition exhibition,
            RoutedEventHandler pictureClickHandler = null)
        {
            BitmapImage cover = null;
            if (exhibition.CoverImageData != null && exhibition.CoverImageData.Length > 0)
            {
                cover = new BitmapImage();
                using (var stream = new System.IO.MemoryStream(exhibition.CoverImageData))
                {
                    cover.BeginInit();
                    cover.CacheOption = BitmapCacheOption.OnLoad;
                    cover.StreamSource = stream;
                    cover.EndInit();
                }
            }

            return template.Render(
                exhibition.Pictures,
                exhibition.Title,
                exhibition.Description,
                cover,
                pictureClickHandler);
        }
    }
}