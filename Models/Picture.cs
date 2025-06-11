using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace Constructor_Gallerix.Models
{
    public class Picture : INotifyPropertyChanged, ICloneable
    {
        private int _id;
        private int _exhibitionId;
        private string _title;
        private string _author;
        private int _year;
        private string _description;
        private string _expertComment;
        private byte[] _imageData;
        private string _orientation;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Id
        {
            get => _id;
            set { _id = value; NotifyPropertyChanged("Id"); }
        }

        public int ExhibitionId
        {
            get => _exhibitionId;
            set { _exhibitionId = value; NotifyPropertyChanged("ExhibitionId"); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; NotifyPropertyChanged("Title"); }
        }

        public string Author
        {
            get => _author;
            set { _author = value; NotifyPropertyChanged("Author"); }
        }

        public int Year
        {
            get => _year;
            set { _year = value; NotifyPropertyChanged("Year"); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public string ExpertComment
        {
            get => _expertComment;
            set { _expertComment = value; NotifyPropertyChanged("ExpertComment"); }
        }

        public byte[] ImageData
        {
            get => _imageData;
            set
            {
                _imageData = value;
                NotifyPropertyChanged("ImageData");
                NotifyPropertyChanged("Image");
            }
        }

        public string Orientation
        {
            get => _orientation;
            set
            {
                _orientation = value;
                NotifyPropertyChanged("Orientation");
                NotifyPropertyChanged("IsHorizontal");
            }
        }

        [Browsable(false)]
        public bool IsHorizontal
        {
            get => Orientation?.ToLower() == "horizontal";
            set => Orientation = value ? "horizontal" : "vertical";
        }

        [Browsable(false)]
        public BitmapImage Image
        {
            get
            {
                try
                {
                    if (_imageData == null || _imageData.Length == 0)
                        return GetDefaultImage();

                    using (var stream = new MemoryStream(_imageData))
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
                    return GetDefaultImage();
                }
            }
        }

        private void NotifyPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public object Clone()
        {
            return new Picture
            {
                Id = this.Id,
                ExhibitionId = this.ExhibitionId,
                Title = this.Title,
                Author = this.Author,
                Year = this.Year,
                Description = this.Description,
                ExpertComment = this.ExpertComment,
                ImageData = this.ImageData,
                Orientation = this.Orientation
            };
        }

        private BitmapImage GetDefaultImage()
        {
            var uri = new Uri("pack://application:,,,/Images/default-image.png", UriKind.Absolute);
            var defaultImage = new BitmapImage(uri);
            defaultImage.Freeze();
            return defaultImage;
        }
    }
}
