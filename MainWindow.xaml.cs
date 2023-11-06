using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Imaging;
using ExifLibrary;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageRate
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        IReadOnlyList<StorageFile> files = null;
        int lastIndex = -1;
        int filter = 0;

        public MainWindow()
        {
            this.InitializeComponent();

            this.AppWindow.SetIcon("Assets/ImageRate_Icon.ico");

            PickFolderButton_Click(null, null);
        }

        private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == Windows.System.VirtualKey.Left)
            {
                loadPrevImg();
            }
            if (args.Key == Windows.System.VirtualKey.Right)
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
            //Rating.Caption = args.Key.ToString();
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous returned file name, if it exists, between iterations of this scenario

            // Create a folder picker
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Initialize the folder picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your folder picker
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a folder
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                BreadcrumbBar.ItemsSource = folder.Path.Split('\\');
                PickFolderButton.Style = null;
                ProgressIndicator.IsActive = true;
                HintText.Text = "";
                files = await folder.GetFilesAsync();
                lastIndex = -1;
                loadNextImg();
            }
            else
            {
                //dialog canceled;
            }
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
                loadImg();
            }

            
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
                        if (getRating(index) >= filter_cached)
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
                loadImg();
            }
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
                        if (getRating(index) >= filter_cached)
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

            if(index < 0)
            {
                return -1;
            }

            if (!files[index].ContentType.StartsWith("image/"))
            {
                return -1;
            }

            var file = ImageFile.FromFile(files[index].Path);

            var rating = file.Properties.Get<ExifUShort>(ExifTag.Rating);
            if (rating != null)
            {
                return rating;
            }
            return 0;
        }

        private void loadImg()
        {

            if (lastIndex < 0)
            {
                return;
            }

            ImageView.Source = new BitmapImage(new Uri(files[lastIndex].Path, UriKind.Absolute));

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


            if(Rating.Value == -1)
            {
                var file = ImageFile.FromFile(files[lastIndex].Path);
                file.Properties.Remove(ExifTag.Rating);
                file.Save(files[lastIndex].Path);
            } else
            {
                var file = ImageFile.FromFile(files[lastIndex].Path);
                file.Properties.Set(ExifTag.Rating, (ushort)sender.Value);
                file.Save(files[lastIndex].Path);
            }


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
        }

    }
}
