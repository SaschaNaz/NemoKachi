﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using NemoKachi;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NemoKachi.CustomElements
{
    public sealed partial class TimelineColumn : UserControl
    {
        public TimelineColumn()
        {
            this.InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AccountTokenCollector collector = Application.Current.Resources["AccountCollector"] as AccountTokenCollector;
            TwitterWrapper.TwitterClient MainClient = Application.Current.Resources["MainClient"] as TwitterWrapper.TwitterClient;
            Parallel.ForEach((this.DataContext as ColumnData).TimelineDatas, async delegate(TwitterWrapper.ITimelineData tlData)
            {
                TwitterWrapper.AccountToken aToken = collector.GetTokenByID(tlData.AccountID);
                await MainClient.RefreshAsync(aToken, tlData);
            });
        }
    }
}
