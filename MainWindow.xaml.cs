﻿using Microsoft.UI.Xaml;
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
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Core;
using Windows.System;

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
            set { SegmentedControl.SelectedIndex = value == ViewMode.List ? 0 : 1; }
        }

        List<ImageItem> listItems = new List<ImageItem>();

        public ObservableCollection<ImageItem> listItemsFiltered { get; } =
            new ObservableCollection<ImageItem>();

        int currentIndex = -1;

        int filter = 0;

        DateTime lastItemClick = DateTime.MinValue;
        ImageItem lastClickedItem = null;

        FullscreenWindow fullscreenWindow;


        public MainWindow()
        {
            this.InitializeComponent();

            this.AppWindow.SetIcon("Assets/ImageRate_Icon.ico");

            string[] cmdargs = Environment.GetCommandLineArgs();
            int len = cmdargs.Length;
            if (len > 0  && (cmdargs[len-1].ToLower().EndsWith(".jpg") || cmdargs[len - 1].ToLower().EndsWith(".jpeg")))
            {
                LoadImagePath(cmdargs[len - 1]);
                currentViewMode = ViewMode.SingleImage;
            }
            else
            {
                loadStorageFolder(KnownFolders.PicturesLibrary);
                currentViewMode = ViewMode.List;
            }

            this.Closed += (s, a) => fullscreenWindow?.Close();

        }

        private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Left && currentViewMode == ViewMode.SingleImage)
            {
                loadPrevImg();
            }
            if (args.Key == VirtualKey.Right && currentViewMode == ViewMode.SingleImage)
            {
                loadNextImg();
            }
            if (args.Key == VirtualKey.Add || (int) args.Key ==  187)
            {
                Rating.Value = Math.Min(Rating.Value + 1, 5);
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == VirtualKey.Subtract || (int) args.Key == 189)
            {
                if (Rating.Value == 1) Rating.Value = -1;
                else Rating.Value -= 1;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == VirtualKey.Number0 || args.Key == VirtualKey.NumberPad0)
            {
                Rating.Value = -1;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == VirtualKey.Number1 || args.Key == VirtualKey.NumberPad1)
            {
                Rating.Value = 1;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == VirtualKey.Number2 || args.Key == VirtualKey.NumberPad2)
            {
                Rating.Value = 2;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == VirtualKey.Number3 || args.Key == VirtualKey.NumberPad3)
            {
                Rating.Value = 3;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == VirtualKey.Number4 || args.Key == VirtualKey.NumberPad4)
            {
                Rating.Value = 4;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == VirtualKey.Number5 || args.Key == VirtualKey.NumberPad5)
            {
                Rating.Value = 5;
                RatingControl_ValueChanged(Rating, null);
            }
            if (args.Key == VirtualKey.L)
            {
                currentViewMode = ViewMode.List;
            }
            if (args.Key == VirtualKey.P)
            {
                currentViewMode = ViewMode.SingleImage;
            }
            if (args.Key == VirtualKey.F11)
            {
                launchFullscreen();
            }
            if (args.Key == VirtualKey.Escape)
            {
                if (fullscreenWindow != null)
                {
                    fullscreenWindow.Close();
                }
            }
            if (args.Key == VirtualKey.Escape)
            {
                if (fullscreenWindow != null)
                {
                    fullscreenWindow.Close();
                }
            }

            //Rating.Caption = args.Key.ToString();
        }

        private async void LoadImagePath(string path)
        {
            string folderPath = path.Substring(0, path.LastIndexOf('\\'));
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            await loadStorageFolder(folder);

            for (int i = 0; i < listItemsFiltered.Count; i++)
            {
                if (listItemsFiltered[i].Path == path) 
                {
                    currentIndex = i;
                    loadImg();
                }
            }
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker openPicker = new FolderPicker();

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add("*");

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
            //StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
            if (folder.Path == "")
            {
                BreadcrumbBar.ItemsSource = new[] { "Picture Library" };
            } else
            {
                BreadcrumbBar.ItemsSource = folder.Path.Split('\\');
                PickFolderButton.Style = null;
            }
            ImageView.Source = null;
            ProgressIndicator.IsActive = true;
            Text_NothingToShow.Visibility = Visibility.Collapsed;

            listItems.Clear();
            listItemsFiltered.Clear();

            IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();
            IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

            for (int i = 0; i < folders.Count; i++)
            {
                var item = new ImageItem(folders[i]);
                listItems.Add(item);
                if (filter == 0)
                {
                    listItemsFiltered.Add(item);
                }
            }

            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].ContentType.StartsWith("image/"))
                {
                    var item = new ImageItem(files[i], listItems.Count);
                    listItems.Add(item);
                    if (filter == 0 || item.Rating >= filter)
                    {
                        listItemsFiltered.Add(item);
                    }
                }
            }

            currentIndex = -1;
            await loadNextImg();
            if (currentIndex == -1)
            {
                ProgressIndicator.IsActive = false;
                if (folders.Count == 0 || filter > 0 || currentViewMode == ViewMode.SingleImage) Text_NothingToShow.Visibility = Visibility.Visible;
            }

        }

        private void launchFullscreen()
        {
            if (fullscreenWindow == null)
            {
                fullscreenWindow = new(this);
                if (currentIndex >= 0)
                {
                    fullscreenWindow.SetCurrentImagePath(listItemsFiltered[currentIndex].Path);
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

        private void Image_RightTapped(object sender, RightTappedRoutedEventArgs e) => 
            ContextMenu.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));

        private void Flyout_ShowInExplorer(object sender, RoutedEventArgs e) =>
            ExternalActionsUtil.ShowInExplorer(listItemsFiltered[currentIndex].Path);

        private void Flyout_OpenWith(object sender, RoutedEventArgs e) => 
            ExternalActionsUtil.OpenWithDialog(listItemsFiltered[currentIndex].Path);

        private void Button_Left(object sender, RoutedEventArgs e)
        {
            loadPrevImg();
        }

        private void Button_Right(object sender, RoutedEventArgs e)
        {
            loadNextImg();
        }

        public async Task loadNextImg()
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
            int index = currentIndex;
            while (true)
            {
                index += 1;
                if (index >= listItemsFiltered.Count)
                {
                    return false;
                }
                else
                {
                    if (filter_cached != filter) return false;

                    if (!listItemsFiltered[index].IsFolder)
                    {
                        currentIndex = index;
                        return true;
                    }
                }
            }
        }

        public async Task loadPrevImg()
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
            int index = currentIndex;
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

                    if (!listItemsFiltered[index].IsFolder)
                    {
                        currentIndex = index;
                        return true;
                    }
                }
            }
        }

        private void loadImg()
        {

            if (currentIndex < 0)
            {
                return;
            }

            if (fullscreenWindow != null)
            {
                fullscreenWindow.SetCurrentImagePath(listItemsFiltered[currentIndex].Path);
            }

            ImageView.Source = new BitmapImage(new Uri(listItemsFiltered[currentIndex].Path, UriKind.Absolute));
            Title = $"ImageRate - {listItemsFiltered[currentIndex].Name}";

            var rating = listItemsFiltered[currentIndex].Rating;
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
            if (ImageView.Source == null || currentIndex < 0)
            {
                Rating.Value = -1;
                return;
            }

            var rating = Math.Max(0, (int)sender.Value);



            var success = listItemsFiltered[currentIndex].updateRating(rating);

            if (!success)
            {
                RatingError.Title = listItemsFiltered[currentIndex].File.ContentType.StartsWith("image/jpeg") ? "Failed to store rating" : "As of now, only *.jpg's are supported for rating";
                RatingError.IsOpen = true;

                // reseting rating to last stored rating
                Rating.Value = listItemsFiltered[currentIndex].Rating;
            }
        }

        private void SegmentedControl_ViewModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentViewMode == ViewMode.List)
            {
                if (currentIndex >= 0)
                {
                    ImagesGridView.SelectedIndex = currentIndex;
                    ImagesGridView.ScrollIntoView(ImagesGridView.SelectedItem);
                }
            }
            if (currentIndex == -1 && listItemsFiltered.Count > 0)
            {
                Text_NothingToShow.Visibility = currentViewMode == ViewMode.List ? Visibility.Collapsed : Visibility.Visible;
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

            

            Text_NothingToShow.Visibility = Visibility.Collapsed;
            ProgressIndicator.IsActive = true;
            ImageView.Source = null;

            var searchIndex = -1;
            if (currentIndex >= 0)
            {
                searchIndex = listItemsFiltered[currentIndex].Index;
            }

            listItemsFiltered.Clear();
            foreach (var item in listItems)
            {
                if (filter == 0 || item.Rating >= filter)
                {
                    listItemsFiltered.Add(item);
                }
            }

            currentIndex = -1;
            for (int i = listItemsFiltered.Count - 1; i >= 0; i--)
            {
                if (currentIndex == -1 || listItemsFiltered[i].Index >= searchIndex)
                {
                    currentIndex = i;
                }
            }

            if (filter > 0 && (currentIndex < 0 || filter > listItemsFiltered[currentIndex].Rating))
            {
                ImageView.Source = null;
                Rating.Value = -1;
                ProgressIndicator.IsActive = false;
                Text_NothingToShow.Visibility = Visibility.Visible;
            } else
            {
                loadImg();
            }

        }

        private void FilterComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            FilterComboBox.SelectedIndex = 0;
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
                args.RegisterUpdateCallback(ShowImageThumbnail);
                args.Handled = true;
            }
        }

        private async void ShowImageThumbnail(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase == 1)
            {
                var templateRoot = args.ItemContainer.ContentTemplateRoot as RelativePanel;
                var image = templateRoot.FindName("ItemImage") as Image;
                var source = templateRoot.FindName("SourceStorage") as TextBlock;
                var item = args.Item as ImageItem;
                source.Text = item.Path;

                var thumbnail = await item.GetImageThumbnailAsync();

                if (item.Path == source.Text) // workaround to check if view already got recycled
                {
                    image.Source = thumbnail;
                }
            }
        }

        private void ImagesGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as ImageItem;

            var shouldSwitch = lastClickedItem == item && (DateTime.Now - lastItemClick).TotalMilliseconds < 750;
            lastClickedItem = item;

            if (!item.IsFolder)
            {
                currentIndex = listItemsFiltered.IndexOf(item);
                loadImg();
            }

            if (shouldSwitch)
            {
                if (item.IsFolder) loadStorageFolder(item.Folder);
                else currentViewMode = ViewMode.SingleImage;
            }
            else lastItemClick = DateTime.Now;
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            launchFullscreen();
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Link;
            } else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var storageFile = items[0];
                    if (storageFile.IsOfType(StorageItemTypes.Folder)) {
                        loadStorageFolder(storageFile as StorageFolder);
                    } else
                    {
                        LoadImagePath(storageFile.Path);
                    }
                }
            }
        }

        private void ImageView_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.Data.SetStorageItems(new[] { listItemsFiltered[currentIndex].File });
            args.Data.RequestedOperation = DataPackageOperation.Copy;
            args.DragUI.SetContentFromDataPackage();
        }

        private void ImagesGridView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var storageItems = new IStorageItem[e.Items.Count];
            for (int i = 0; i < e.Items.Count; i++)
            {
                var item = (e.Items[i] as ImageItem);
                storageItems[i] = item.IsFolder ? item.Folder : item.File;
            }

            e.Data.SetStorageItems(storageItems);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }

        private async void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            var items = BreadcrumbBar.ItemsSource as String[];
            var folderPath = String.Join("\\", items, 0, args.Index + 1);
            await loadStorageFolder(await StorageFolder.GetFolderFromPathAsync(folderPath));
        }
    }
}
