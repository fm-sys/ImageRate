using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.FileProperties;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.ComponentModel;
using Microsoft.UI.Windowing;
using ImageRate.Assets;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageRate
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        enum ViewMode
        {
            SingleImage,
            List
        }

        ViewMode currentViewMode
        {
            get { return SegmentedControl.SelectedIndex == 0 ? ViewMode.List : ViewMode.SingleImage; }
        }


        List<ImageItem> listItems = new List<ImageItem>();

        public ObservableCollection<ImageItem> listItemsFiltered { get; } =
            new ObservableCollection<ImageItem>();

        IReadOnlyList<StorageFile> files = null;
        int[] ratings = null;
        int lastIndex = -1;
        int filter = 0;

        FullscreenWindow fullscreenWindow;

        public MainWindow()
        {
            this.InitializeComponent();

            this.AppWindow.SetIcon("Assets/ImageRate_Icon.ico");

            string[] cmdargs = Environment.GetCommandLineArgs();
            int len = cmdargs.Length;
            if (len > 0  && (cmdargs[len-1].ToLower().EndsWith(".jpg") || cmdargs[len - 1].ToLower().EndsWith(".jpeg")))
            {
                LoadPath(cmdargs[len - 1]);
            } else
            {
                PickFolderButton_Click(null, null);
            }

        }

        private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == Windows.System.VirtualKey.Left && currentViewMode == ViewMode.SingleImage)
            {
                loadPrevImg();
            }
            if (args.Key == Windows.System.VirtualKey.Right && currentViewMode == ViewMode.SingleImage)
            {
                loadNextImg();
            }
            if (args.Key == Windows.System.VirtualKey.Add || (int) args.Key ==  187)
            {
                Rating.Value = Math.Min(Rating.Value + 1, 5);
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == Windows.System.VirtualKey.Subtract || (int) args.Key == 189)
            {
                if (Rating.Value == 1) Rating.Value = -1;
                else Rating.Value -= 1;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == Windows.System.VirtualKey.Number0 || args.Key == Windows.System.VirtualKey.NumberPad0)
            {
                Rating.Value = -1;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == Windows.System.VirtualKey.Number1 || args.Key == Windows.System.VirtualKey.NumberPad1)
            {
                Rating.Value = 1;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == Windows.System.VirtualKey.Number2 || args.Key == Windows.System.VirtualKey.NumberPad2)
            {
                Rating.Value = 2;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == Windows.System.VirtualKey.Number3 || args.Key == Windows.System.VirtualKey.NumberPad3)
            {
                Rating.Value = 3;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == Windows.System.VirtualKey.Number4 || args.Key == Windows.System.VirtualKey.NumberPad4)
            {
                Rating.Value = 4;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == Windows.System.VirtualKey.Number5 || args.Key == Windows.System.VirtualKey.NumberPad5)
            {
                Rating.Value = 5;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == Windows.System.VirtualKey.L)
            {
                SegmentedControl.SelectedIndex = 0;
            }
            if (args.Key == Windows.System.VirtualKey.P)
            {
                SegmentedControl.SelectedIndex = 1;
            }
            if (args.Key == Windows.System.VirtualKey.F11)
            {
                launchFullscreen();
            }
            if (args.Key == Windows.System.VirtualKey.Escape)
            {
                if (fullscreenWindow != null)
                {
                    fullscreenWindow.Close();
                }
            }

            //Rating.Caption = args.Key.ToString();
        }

        private async void LoadPath(string path)
        {
            string folderPath = path.Substring(0, path.LastIndexOf('\\'));
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            await loadStorageFolder(folder);

            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].Path == path) 
                {
                    lastIndex = i;
                    loadImg();
                }
            }
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous returned file name, if it exists, between iterations of this scenario

            // Create a folder picker
            FolderPicker openPicker = new FolderPicker();

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Initialize the folder picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your folder picker
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a folder
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                await loadStorageFolder(folder);
            }
            else
            {
                //dialog canceled;
            }
        }

        private async Task loadStorageFolder(StorageFolder folder)
        {
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
            BreadcrumbBar.ItemsSource = folder.Path.Split('\\');
            ImageView.Source = null;
            PickFolderButton.Style = null;
            ProgressIndicator.IsActive = true;
            HintText.Text = "";
            files = await folder.GetFilesAsync();
            ratings = new int[files.Count];
            for (int i = 0; i < files.Count; i++)
            {
                ratings[i] = -1;
            }
            lastIndex = -1;
            await loadNextImg();
            if (lastIndex == -1)
            {
                ProgressIndicator.IsActive = false;
                HintText.Text = "Nothing to show";
            }
            loadRatingsAndList();
        }

        private void launchFullscreen()
        {
            if (fullscreenWindow == null)
            {
                fullscreenWindow = new();
                if (lastIndex >= 0)
                {
                    fullscreenWindow.SetCurrentImagePath(files[lastIndex].Path);
                }
                fullscreenWindow.AppWindow.Move(this.AppWindow.Position);
                fullscreenWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                fullscreenWindow.Closed += (sender, args) => fullscreenWindow = null;
            }
            fullscreenWindow.Activate();
        }

        private void updateViewMode()
        {
            var visibleSingleImage = currentViewMode == ViewMode.SingleImage ? Visibility.Visible : Visibility.Collapsed;
            var visibleList = currentViewMode == ViewMode.List ? Visibility.Visible : Visibility.Collapsed;

            Button_Prev.Visibility = visibleSingleImage;
            Button_Next.Visibility = visibleSingleImage;
            ImageView.Visibility = visibleSingleImage;
            ProgressIndicator.Visibility = visibleSingleImage;
            Rating.Visibility = visibleSingleImage;
            InfoNoMoreImages.Visibility = visibleSingleImage;

            ImagesGridView.Visibility = visibleList;
        }

        private void Image_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ContextMenu.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void Flyout_ShowInExplorer(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/select," + files[lastIndex].Path);
        }

        private void Flyout_OpenWith(object sender, RoutedEventArgs e)
        {
            Process proc = new Process();
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = "rundll32.exe";
            proc.StartInfo.Arguments = "shell32,OpenAs_RunDLL " + files[lastIndex].Path;
            proc.Start();
        }

        private void loadRatingsAndList()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                listItems.Clear();
                listItemsFiltered.Clear();
            });
            Task.Run(() =>
            {
                Thread.Sleep(200); // nobody knows why, but this seem to fix the
                                   // uncatchable crash while loading the thumbnail
                                   //
                                   // related?: https://github.com/microsoft/microsoft-ui-xaml/issues/2386


                for (int i = 0; i < files.Count; i++)
                {
                    if (files[i].ContentType.StartsWith("image/"))
                    {
                        var rating = getRating(i);
                        var item = new ImageItem(files[i], rating, i);
                        DispatcherQueue.TryEnqueue(() => {
                            listItems.Add(item);
                            if (filter == 0 || item.Rating >= filter)
                            {
                                listItemsFiltered.Add(item);
                            }
                        });
                    }
                }
            });

        }

        private void Button_Left(object sender, RoutedEventArgs e)
        {
            loadPrevImg();
        }

        private void Button_Right(object sender, RoutedEventArgs e)
        {
            loadNextImg();
        }

        private async Task loadNextImg()
        {

            bool result = await Task.Run(() => searchNextImg());
            if (result)
            {
                // close possible error message
                RatingError.IsOpen = false;

                loadImg();
            }
            InfoNoMoreImages.IsOpen = !result;



        }

        private bool searchNextImg()
        {
            int filter_cached = filter;
            int index = lastIndex;
            while (true)
            {
                index += 1;
                if (files == null || index >= files.Count)
                {
                    return false;
                }
                else
                {
                    if (filter_cached != filter) return false;

                    if (files[index].ContentType.StartsWith("image/"))
                    {
                        if (filter_cached == 0 || getRating(index) >= filter_cached)
                        {
                            lastIndex = index;
                            return true;
                        }
                    }
                }
            }
        }

        private async Task loadPrevImg()
        {
            bool result = await Task.Run(() => searchPrevImg());
            if (result)
            {
                // close possible error message
                RatingError.IsOpen = false;

                loadImg();
            }
            InfoNoMoreImages.IsOpen = !result;

        }

        private bool searchPrevImg()
        {
            int filter_cached = filter;
            int index = lastIndex;
            while (true)
            {
                index -= 1;
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    if (filter_cached != filter) return false;

                    if (files[index].ContentType.StartsWith("image/"))
                    {
                        if (filter_cached == 0 || getRating(index) >= filter_cached)
                        {
                            lastIndex = index;
                            return true;
                        }
                    }
                }
            }
        }

        private int getRating(int index)
        {
            if (index < 0 || index >= files.Count)
            {
                return -1;
            }

            if (ratings.Length > index && ratings[index] >= 0)
            {
                return ratings[index];
            }
            {

            }

            if (!files[index].ContentType.StartsWith("image/")) // "image/jpeg"? file formats other than jpg doesn't suport rating
            {
                return -1;
            }

            try
            {
                Task.Run(async () =>
                {
                    ImageProperties properties = await files[index].Properties.GetImagePropertiesAsync();
                    var ratingPerc = properties.Rating;
                    ratings[index] = ratingPerc == 0 ? 0 : (int)Math.Round((double)ratingPerc / 25.0) + 1;
                }).Wait();
            }
            catch
            {
                RatingError.Title = "Failed to read rating";
                RatingError.IsOpen = true;
            }

            return ratings[index];
        }

        private void loadImg()
        {

            if (lastIndex < 0)
            {
                return;
            }

            if (fullscreenWindow != null)
            {
                fullscreenWindow.SetCurrentImagePath(files[lastIndex].Path);
            }

            ImageView.Source = new BitmapImage(new Uri(files[lastIndex].Path, UriKind.Absolute));
            Title = $"ImageRate - {files[lastIndex].Name}";

            var rating = getRating(lastIndex);
            if (rating != 0)
            {
                Rating.Value = rating;
            }
            else
            {
                Rating.Value = -1;
            }
        }

        private void RatingControl_ValueChanged(RatingControl sender, object args)
        {
            if (ImageView.Source == null)
            {
                Rating.Value = -1;
                return;
            }

            var rating = Math.Max(0, (int)sender.Value);

            var ratingPerc = rating switch {
                1 => 1,
                2 => 25,
                3 => 50,
                4 => 75,
                5 => 99,
                _ => 0 
            };

            try
            {
                Task.Run(async () =>
                {
                    ImageProperties properties = await files[lastIndex].Properties.GetImagePropertiesAsync();
                    properties.Rating = (uint)ratingPerc;
                    await properties.SavePropertiesAsync();
                }).Wait();
                ratings[lastIndex] = rating;

                foreach (var item in listItemsFiltered)
                {
                    if (item.Index == lastIndex)
                    {
                        item.Rating = rating;
                        break;
                    }
                }
            }
            catch
            {
                RatingError.Title = files[lastIndex].ContentType.StartsWith("image/jpeg") ? "Failed to store rating" : "As of now, only *.jpg's are supported for rating";
                RatingError.IsOpen = true;

                // reseting rating to last stored rating
                Rating.Value = ratings[lastIndex] == 0 ? -1 : ratings[lastIndex];
            }


        }

        private void SegmentedControl_ViewModeChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = SegmentedControl.SelectedIndex;
            if (selected == 0)
            {
                if (lastIndex >= 0)
                {
                    for (int i = 0; i < listItemsFiltered.Count; i++)
                    {
                        if (listItemsFiltered[i].Index == lastIndex)
                        {
                            ImagesGridView.SelectedIndex = lastIndex;
                            ImagesGridView.ScrollIntoView(ImagesGridView.SelectedItem);
                            break;
                        }
                    }
                }
            }
            updateViewMode();
        }

        private async void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string filterString = e.AddedItems[0].ToString();
            switch (filterString)
            {
                case "No Filter":
                    filter = 0;
                    break;
                case "≥ 1 star":
                    filter = 1;
                    break;
                case "≥ 2 stars":
                    filter = 2;
                    break;
                case "≥ 3 stars":
                    filter = 3;
                    break;
                case "≥ 4 stars":
                    filter = 4;
                    break;
                case "= 5 stars":
                    filter = 5;
                    break;
                default:
                    throw new Exception($"Invalid argument: {filterString}");
            }

            if(files == null)
            {
                return;
            }

            HintText.Text = "";
            ProgressIndicator.IsActive = true;
            ImageView.Source = null;

            listItemsFiltered.Clear();
            foreach (var item in listItems)
            {
                if (filter == 0 || item.Rating >= filter)
                {
                    listItemsFiltered.Add(item);
                }
            }


            if (filter > getRating(lastIndex))
            {
                await loadNextImg();
            }
            if (filter > getRating(lastIndex))
            {
                await loadPrevImg();
            }
            if (filter > getRating(lastIndex))
            {
                ImageView.Source = null;
                Rating.Value = -1;
                ProgressIndicator.IsActive = false;
                HintText.Text = "Nothing to show";
            } else
            {
                loadImg();
            }

        }

        private void FilterComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            FilterComboBox.SelectedIndex = 0;
            SegmentedControl.SelectedIndex = 1;
        }

        private void ImageGridView_ContainerContentChanging(object sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
            {
                var templateRoot = args.ItemContainer.ContentTemplateRoot as RelativePanel;
                var image = templateRoot.FindName("ItemImage") as Image;
                image.Source = null;
            }

            if (args.Phase == 0)
            {
                args.RegisterUpdateCallback(ShowImage);
                args.Handled = true;
            }
        }

        private async void ShowImage(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase == 1)
            {
                var templateRoot = args.ItemContainer.ContentTemplateRoot as RelativePanel;
                var image = templateRoot.FindName("ItemImage") as Image;
                var source = templateRoot.FindName("SourceStorage") as TextBlock;
                var item = args.Item as ImageItem;
                source.Text = item.Source;

                var thumbnail = await item.GetImageThumbnailAsync();

                if (item.Source == source.Text) // workaround to check if view already got recycled
                {
                    image.Source = thumbnail;
                }
            }
        }

        private void ImagesGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as ImageItem;

            var shouldSwitch = lastIndex == item.Index;

            lastIndex = item.Index;
            loadImg();

            if (shouldSwitch) SegmentedControl.SelectedIndex = 1;
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            launchFullscreen();
        }
    }

}
