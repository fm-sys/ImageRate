using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace ImageRate


{
    public class ImageItem : INotifyPropertyChanged
    {

        private static SemaphoreSlim IOWriteLock = new SemaphoreSlim(1, 1); 

        private int rating;
        private int index;
        private StorageFile file;
        private StorageFolder folder;
        private BitmapImage cachedThumbnail;
        private bool isFolder;

        public ImageItem(StorageFile file, int index)
        {
            this.file = file;
            this.rating = -1;
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

        private void initRatingFromStorageFile()
        {
            if (isFolder) return;

            Task.Run(async () =>
            {
                ImageProperties properties = await file.Properties.GetImagePropertiesAsync();
                var ratingPerc = properties.Rating;
                rating = ratingPerc == 0 ? 0 : (int)Math.Round((double)ratingPerc / 25.0) + 1;
            }).Wait();
        }

        /// <param name="newRating">the new rating 0-5</param>
        /// <returns>true, if rating was successfully stored</returns>
        public async Task<bool> updateRating(int newRating)
        {
            if (isFolder) return false;

            var ratingPerc = newRating switch
            {
                1 => 1,
                2 => 25,
                3 => 50,
                4 => 75,
                5 => 99,
                _ => 0
            };

            await IOWriteLock.WaitAsync();
            try
            {
                ImageProperties properties = await file.Properties.GetImagePropertiesAsync();
                properties.Rating = (uint)ratingPerc;
                await properties.SavePropertiesAsync();

                rating = newRating;
                OnPropertyChanged("Rating");
                return true;
            }
            catch (Exception e)
            {
                return false;
            } 
            finally { IOWriteLock.Release(); }

        }

        public int Rating
        {
            get { 
                if (rating == -1) initRatingFromStorageFile();

                return rating == 0 ? -1 : rating; 
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

        public String Path
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

            await Task.Delay(100); // keep this! It seems to somehow fix a race condition crash when calling GetThumbnailAsync

            var bitmapImage = new BitmapImage();

            StorageItemThumbnail thumbnail;
            if (isFolder)
            {
                thumbnail = await folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 180, ThumbnailOptions.ResizeThumbnail);
            } 
            else
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
