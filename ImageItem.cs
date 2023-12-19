using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace ImageRate


{
    public class ImageItem
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
            set { rating = value; }
        }

        public int Index
        {
            get { return index; }
        }

        public StorageFile File
        {
            get { return file; }
            set { file = value; }
        }

        public String Source
        {
            get { return file.Path; }
        }

        public async Task<BitmapImage> GetImageThumbnailAsync()
        {
            var bitmapImage = new BitmapImage();

            // I tried to try/catch this but it didn't fix the problem: https://github.com/microsoft/microsoft-ui-xaml/issues/2386
            Console.WriteLine(file.Path);
            StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.PicturesView);
            bitmapImage.SetSource(thumbnail);
            thumbnail.Dispose();
            
            return bitmapImage;
        }

        public String Name
        {
            get { return file.Name; }
        }
    }
}
