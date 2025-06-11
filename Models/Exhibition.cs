using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Windows.Media.Imaging;

namespace Constructor_Gallerix.Models
{
    public class Exhibition
    {
        public int ExhibitionId { get; set; }
        public int UserId { get; set; }

        [Required(ErrorMessage = "Название выставки обязательно")]
        [StringLength(150, ErrorMessage = "Название не должно превышать 150 символов")]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Template { get; set; }

        public byte[] CoverImageData { get; set; }

        [NotMapped]
        public List<Picture> Pictures { get; set; } = new List<Picture>();

        [NotMapped]
        public BitmapImage CoverImage
        {
            get
            {
                try
                {
                    if (CoverImageData == null || CoverImageData.Length == 0)
                        return new BitmapImage(new Uri("pack://application:,,,/Images/default-image.png", UriKind.Absolute));

                    using (var stream = new MemoryStream(CoverImageData))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                        return image;
                    }
                }
                catch
                {
                    // В случае ошибки вернуть дефолтную картинку
                    return new BitmapImage(new Uri("pack://application:,,,/Images/default-image.png", UriKind.Absolute));
                }
            }
        }

        [NotMapped]
        public BitmapImage FavoriteIcon
        {
            get
            {
                return new BitmapImage(new Uri(
                    IsFavorite
                        ? "pack://application:,,,/Images/heart-filled.png"
                        : "pack://application:,,,/Images/heart-empty.png", UriKind.Absolute));
            }
        }

        [NotMapped]
        public BitmapImage VisibilityIcon
        {
            get
            {
                return new BitmapImage(new Uri(
                    IsPublic
                        ? "pack://application:,,,/Images/eye-open.png"
                        : "pack://application:,,,/Images/eye-closed.png", UriKind.Absolute));
            }
        }
    }
}
