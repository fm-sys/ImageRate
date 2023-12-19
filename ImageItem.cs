using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace ImageRate


{
    public class ImageItem : INotifyPropertyChanged
    {

        private int rating;
        private int index;
        private StorageFile file;

        public ImageItem(StorageFile file, int rating, int index)
        {
            this.file = file;
            this.rating = rating;
            this.index = index;
        }

        public int Rating
        {
            get { return rating == 0 ? -1 : rating; }
            set { 
                rating = value;
                OnPropertyChanged();
            }
        }

        public int Index
        {
            get { return index; }
        }

        public StorageFile File
        {
            get { return file; }
        }

        public String Source
        {
            get { return file.Path; }
        }

        public async Task<BitmapImage> GetImageThumbnailAsync()
        {
            var bitmapImage = new BitmapImage();

            StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 180, ThumbnailOptions.ResizeThumbnail);
            bitmapImage.SetSource(thumbnail);
            thumbnail.Dispose();
            
            return bitmapImage;
        }

        public String Name
        {
            get { return file.Name; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
