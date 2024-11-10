using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
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

        private TimeSpan startTimestamp;


        public ImageItem(StorageFile file)
        {
            this.file = file;
            this.rating = -1;
            this.index = -1;
            this.isFolder = false;
            this.startTimestamp = TimeSpan.Zero;  // Default start timestamp for videos, for other media types this value is irrelevant
        }

        public ImageItem(StorageFolder folder)
        {
            this.folder = folder;
            this.rating = 0;
            this.index = -1;
            this.isFolder = true;
        }

        private async Task<(int, TimeSpan)> LoadRatingFromCompanionFileAsync()
        {
            string companionFileName = file.Path.Substring(0, file.Path.LastIndexOf('.')) + ".imagerate";
            try
            {
                var companionFile = await StorageFile.GetFileFromPathAsync(companionFileName);
                string jsonText = await FileIO.ReadTextAsync(companionFile);
                var jsonDoc = JsonDocument.Parse(jsonText);

                int loadedRating = 0;
                TimeSpan loadedStartTimestamp = TimeSpan.Zero;

                if (jsonDoc.RootElement.TryGetProperty("Rating", out JsonElement ratingElement))
                {
                    loadedRating = ratingElement.GetInt32();
                }
                if (jsonDoc.RootElement.TryGetProperty("StartTimestamp", out JsonElement timestampElement))
                {
                    loadedStartTimestamp = TimeSpan.FromSeconds(timestampElement.GetDouble());
                }

                return (loadedRating, loadedStartTimestamp);
            }
            catch (FileNotFoundException)
            {
                return (0, TimeSpan.Zero); // Companion file doesn't exist
            }
            catch
            {
                // Ignore other errors and assume rating is 0 with no timestamp
            }
            return (0, TimeSpan.Zero);
        }

        private void initRatingFromStorageFile()
        {
            if (isFolder) return;

            Task.Run(async () =>
            {
                if (file.ContentType.StartsWith("image/jpeg"))
                {
                    ImageProperties properties = await file.Properties.GetImagePropertiesAsync();
                    var ratingPerc = properties.Rating;
                    rating = ratingPerc == 0 ? 0 : (int)Math.Round((double)ratingPerc / 25.0) + 1;
                }
                else
                {
                    var (loadedRating, loadedStartTimestamp) = await LoadRatingFromCompanionFileAsync();
                    rating = loadedRating;
                    startTimestamp = loadedStartTimestamp;
                }
            }).Wait();
        }

        /// <param name="newRating">the new rating 0-5</param>
        /// <returns>true, if rating was successfully stored</returns>
        public async Task<bool> updateRating(int newRating)
        {
            if (isFolder) return false;

            if (file.ContentType.StartsWith("image/jpeg"))
            {
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
                catch
                {
                    return false;
                }
                finally { IOWriteLock.Release(); }
            }
            else
            {
                return await storeCompanion(newRating, startTimestamp);
            }


        }

        public async Task<bool> updateStartTimestamp(TimeSpan newStartTimestamp)
        {
            return await storeCompanion(rating, newStartTimestamp);
        }

            private async Task<bool> storeCompanion(int newRating, TimeSpan newStartTimestamp)
        {

            string companionFileName = file.Name.Substring(0, file.Name.LastIndexOf('.')) + ".imagerate";

            var companionData = new
            {
                Rating = newRating,
                StartTimestamp = newStartTimestamp.TotalSeconds
            };
            string json = JsonSerializer.Serialize(companionData);

            try
            {
                var companionFile = await(await file.GetParentAsync()).CreateFileAsync(
                    companionFileName,
                    CreationCollisionOption.ReplaceExisting
                );

                if (newRating == 0 & startTimestamp == TimeSpan.Zero)
                {
                    await companionFile.DeleteAsync();
                } else
                {
                    await FileIO.WriteTextAsync(companionFile, json);
                }

                rating = newRating;
                startTimestamp = newStartTimestamp;
                OnPropertyChanged("Rating");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public TimeSpan StartTimestamp
        {
            get
            {
                //if (rating == -1) initRatingFromStorageFile();
                return startTimestamp;
            }
        }

        public int Rating
        {
            get
            {
                if (rating == -1) initRatingFromStorageFile();

                return rating == 0 ? -1 : rating;
            }
        }

        public DateTimeOffset? DateTaken
        {
            get
            {
                if (isFolder) return null;

                DateTimeOffset? dateTaken = null;

                Task.Run(async () =>
                {
                    ImageProperties properties = await file.Properties.GetImagePropertiesAsync();
                    dateTaken = properties.DateTaken;
                }).Wait();

                return dateTaken;
            }
        }

        public int Index
        {
            get { return index; }
            set { index = value; }
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
            get
            {
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
            get
            {
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
