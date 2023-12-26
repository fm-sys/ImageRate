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

namespace AppUIBasics.ControlPages
{
    public sealed partial class DelaySettingsDialogContent : Page
    {
        public DelaySettingsDialogContent(int initialValue)
        {
            this.InitializeComponent();
            DurationSlider.Value = initialValue;
        }

        public int getDuration()
        {
            return (int) DurationSlider.Value;
        }
    }
}
