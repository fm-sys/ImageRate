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

            Closed += (o,w) => autoplay_timer?.Dispose();

        }

        private void initMonitorFlyout()
        {
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

        public void SetCurrentImagePath(String path)
        {
            var ToImageView = img_1_active ? ImageView2 : ImageView1;
            var FromImageView = img_1_active ? ImageView1 : ImageView2;

            img_1_active = !img_1_active;

            ToImageView.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
            ToImageView.Opacity = 1;
            FromImageView.Opacity = 0;

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

        private async Task sheduleAutoplayTimer()
        {
            fromWindow.loadNextImg();
            autoplay_timer = new PeriodicTimer(TimeSpan.FromSeconds(6));
            while (await autoplay_timer.WaitForNextTickAsync())
            {
                fromWindow.loadNextImg();
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
    }
}
