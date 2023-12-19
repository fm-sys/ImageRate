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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageRate.Assets
{

    public sealed partial class FullscreenWindow : Window
    {
        bool img_1_active = false;

        public FullscreenWindow()
        {
            this.InitializeComponent();

            this.AppWindow.SetIcon("Assets/ImageRate_Icon.ico");
        }

        private void FullscreenWindow_KeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == Windows.System.VirtualKey.Escape)
            {
                Close();
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

    }
}
