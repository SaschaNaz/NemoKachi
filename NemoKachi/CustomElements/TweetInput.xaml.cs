using System;
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
using NemoKachi.TwitterWrapper.TwitterDatas;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NemoKachi.CustomElements
{
    public sealed partial class TweetInput : UserControl
    {
        public TweetInput()
        {
            this.InitializeComponent();
            DataContext = this;
        }

        public Tweet ReplyTweet
        {
            get
            {
                return (Tweet)GetValue(ReplyTweetProperty);
            }
            set
            {
                SetValue(ReplyTweetProperty, value);
            }
        }

        public static readonly DependencyProperty ReplyTweetProperty =
            DependencyProperty.Register("ReplyTweet",
            typeof(Tweet),
            typeof(TweetInput),
            new PropertyMetadata(null, new Windows.UI.Xaml.PropertyChangedCallback(ReplyTweetChangedCallback)));

        private static void ReplyTweetChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TweetInput input = d as TweetInput;

            if (e.NewValue != null)
                input.ReplyGrid.Visibility = Visibility.Visible;
            else
                input.ReplyGrid.Visibility = Visibility.Collapsed;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AccountTokenCollector collector = Application.Current.Resources["AccountCollector"] as AccountTokenCollector;
            TwitterWrapper.TwitterClient MainClient = Application.Current.Resources["MainClient"] as TwitterWrapper.TwitterClient;
            TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;

            String sendText;
            SendTextBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out sendText);
            TwitterWrapper.SendTweetRequest tweetrequest = new TwitterWrapper.SendTweetRequest()
                {
                    status = sendText
                };
            if (ReplyTweet != null)
                tweetrequest.in_reply_to_status_id = ReplyTweet.Id;
            TwitterWrapper.TwitterDatas.Tweet twt = await MainClient.SendTweetAsync(
                collector.TokenCollection[0],
                tweetrequest);
            house.StoreTweets(twt);
        }
    }
}
