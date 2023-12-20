using Microsoft.UI.Xaml;
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
        private StorageFolder folder;
        private BitmapImage cachedThumbnail;
        private bool isFolder;

        public ImageItem(StorageFile file, int rating, int index)
        {
            this.file = file;
            this.rating = rating;
            this.index = index;
            this.isFolder = false;
        }

        public ImageItem(StorageFolder folder)
        {
            this.folder = folder;
            this.rating = 0;
            this.index = -1;
            this.isFolder = true;
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

        public StorageFolder Folder
        {
            get { return folder; }
        }

        public String Source
        {
            get { 
                if (isFolder)
                {
                    return folder.Path;
                }
                return file.Path; 
            }
        }

        public async Task<BitmapImage> GetImageThumbnailAsync()
        {
            if (cachedThumbnail != null)
            {
                return cachedThumbnail;
            }

            var bitmapImage = new BitmapImage();

            StorageItemThumbnail thumbnail;
            if (isFolder)
            {
                thumbnail = await folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 180, ThumbnailOptions.ResizeThumbnail);
            } else
            {
                thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 180, ThumbnailOptions.ResizeThumbnail);
            }
            bitmapImage.SetSource(thumbnail);
            thumbnail.Dispose();

            cachedThumbnail = bitmapImage;
            return bitmapImage;
        }

        public String Name
        {
            get { 
                if (isFolder)
                {
                    return folder.Name;
                }
                return file.Name; 
            }
        }

        public bool IsFolder { get { return isFolder; } }

        public Visibility VisibleIfFolder
        {
            get { return isFolder ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility VisibleIfFile
        {
            get { return isFolder ? Visibility.Collapsed : Visibility.Visible; }
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
