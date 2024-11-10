using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;
using AppUIBasics.ControlPages;
using static System.Net.Mime.MediaTypeNames;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Graphics.Display;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageRate.Assets
{

    public sealed partial class FullscreenWindow : Window
    {
        bool img_1_active = false;
        PeriodicTimer autoplay_timer = null;
        MainWindow fromWindow;

        public FullscreenWindow(MainWindow fromWindow)
        {
            InitializeComponent();

            AppWindow.SetIcon("Assets/ImageRate_Icon.ico");

            this.fromWindow = fromWindow;

            initMonitorFlyout();

            //ProjectionManager.
            

            VideoView.SetMediaPlayer(new MediaPlayer());
            VideoView.MediaPlayer.Volume = 0;

            fromWindow.mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            fromWindow.mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;


            Closed += (o,w) => { 
                autoplay_timer?.Dispose();

                fromWindow.mediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                fromWindow.mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            };

        }

        private void initMonitorFlyout()
        {
            var monitors = MonitorHelper.GetAllMonitorsInfo();

            for (int i = 0; i < monitors.Count; i++)
            {
                var monitor = monitors[i];
                var menuItem = new ToggleMenuFlyoutItem();
                menuItem.Text = monitor.DeviceName;
                MonitorSubmenu.Items.Add(menuItem);
                menuItem.Click += (s, e) => {
                    AppWindow.Move(new PointInt32(monitor.Monitor.Left, monitor.Monitor.Top));
                    AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                    foreach(var item in MonitorSubmenu.Items)
                    {
                        (item as ToggleMenuFlyoutItem).IsChecked = false;
                    }
                    menuItem.IsChecked = true;
                };
                if (i == monitors.Count - 1)
                {
                    menuItem.IsChecked = true;

                    AppWindow.Move(new PointInt32(monitor.Monitor.Left, monitor.Monitor.Top));
                    AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                }
            }
            
        }
        /*{
            
            var areas = DisplayArea.FindAll();

            for (int i = 0; i < areas.Count; i++)
            {
                DisplayArea item = areas[i];
                MenuFlyoutItem menuItem = new MenuFlyoutItem();
                menuItem.Text = $"Monitor {i + 1}";
                if (item.IsPrimary) menuItem.Text += " (primary)";
                MonitorSubmenu.Items.Add(menuItem);
                menuItem.Click += (s, e) => {
                    AppWindow.Move(new PointInt32(item.OuterBounds.X, item.OuterBounds.Y));
                    AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                };
            }

            Screen.AllScreens

        }*/

        // Sync the playback state of mediaPlayer2 with mediaPlayer1
        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            var dispatcherQueue = DispatcherQueue.TryEnqueue(() =>
            {
                switch (sender.PlaybackState)
                {
                    case MediaPlaybackState.Playing:
                        VideoView.MediaPlayer.Play();
                        break;
                    case MediaPlaybackState.Paused:
                        VideoView.MediaPlayer.Pause();
                        break;
                }
            });
            
        }

        // Sync the playback position of mediaPlayer2 with mediaPlayer1
        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            var dispatcherQueue = DispatcherQueue.TryEnqueue(() =>
            {
                if (VideoView.MediaPlayer != null &&
                    Math.Abs((sender.Position - VideoView.MediaPlayer.PlaybackSession.Position).TotalMilliseconds) > 50)
                {
                    VideoView.MediaPlayer.PlaybackSession.Position = sender.Position;
                }
            });
        }


        private void FullscreenWindow_KeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == Windows.System.VirtualKey.Left)
            {
                fromWindow.loadPrevImg();
            }
            if (args.Key == Windows.System.VirtualKey.Right)
            {
                fromWindow.loadNextImg();
            }
            if (args.Key == Windows.System.VirtualKey.Escape)
            {
                Close();
            }
            if (args.Key == Windows.System.VirtualKey.Space)
            {
                toggleAutoplay();
            }
        }

        public void SetCurrentImagePath(ImageItem item)
        {
            var itemPath = new Uri(item.Path, UriKind.Absolute);

            if (item.File.ContentType.StartsWith("video/"))
            {
                VideoView.MediaPlayer.Source = MediaSource.CreateFromUri(itemPath);
                VideoView.Opacity = 1;
                ImageView1.Opacity = 0;             
                ImageView2.Opacity = 0;
            }
            else
            {
                VideoView.Source = null;
                VideoView.Opacity = 0;

                var ToImageView = img_1_active ? ImageView2 : ImageView1;
                var FromImageView = img_1_active ? ImageView1 : ImageView2;

                img_1_active = !img_1_active;

                ToImageView.Source = new BitmapImage(itemPath);
                ToImageView.Opacity = 1;
                FromImageView.Opacity = 0;
            }

            

        }

        public void toggleAutoplay()
        {
            if (autoplay_timer != null)
            {
                autoplay_timer.Dispose();
                autoplay_timer = null;
                AutoplayToogle.IsChecked = false;
            }
            else
            {
                sheduleAutoplayTimer();
                AutoplayToogle.IsChecked = true;
            }
        }

        private async void changeDelay()
        {
            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = Content.XamlRoot;
            dialog.Title = "Set image duration (in seconds)";
            dialog.CloseButtonText = "OK";
            dialog.DefaultButton = ContentDialogButton.Close;
            var content = new DelaySettingsDialogContent(getDelay());
            dialog.Content = content;
            await dialog.ShowAsync();

            SettingsHelper.set("dia_delay", content.getDuration());

            if (autoplay_timer != null)
            {
                autoplay_timer.Dispose();
                autoplay_timer = null;
                toggleAutoplay();
            }
        }

        private int getDelay()
        {
            return SettingsHelper.getIntOrDefault("dia_delay", 6);
        }

        private async Task sheduleAutoplayTimer()
        {
            fromWindow.loadNextImg();

            
            autoplay_timer = new PeriodicTimer(TimeSpan.FromSeconds(getDelay()));

            while (await autoplay_timer.WaitForNextTickAsync())
            {
                if (VideoView.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing) fromWindow.loadNextImg();
            }
        }

        private void MenuFlyoutItem_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ContextMenu.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void MenuFlyoutItem_ToogleAutoplay(object sender, RoutedEventArgs e)
        {
            toggleAutoplay();
        }

        private void MenuFlyoutItem_ConfigureAutoplay(object sender, RoutedEventArgs e)
        {
            changeDelay();
        }
    }
}
