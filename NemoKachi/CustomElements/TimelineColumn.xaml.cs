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
using System.Threading.Tasks;
using NemoKachi;
using NemoKachi.TwitterWrapper.TwitterDatas;

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
            TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;
            ColumnData ContextData = this.DataContext as ColumnData;

            Parallel.ForEach((this.DataContext as ColumnData).TimelineDatas, async delegate(TwitterWrapper.ITimelineData tlData)
            {
                String message = null;
                try
                {
                    TwitterWrapper.AccountToken aToken = collector.GetTokenByID(tlData.AccountID);

                    Tweet[] tweets = await MainClient.RefreshAsync(aToken, tlData);
                    if (tweets.Length != 0)
                    {
                        //tlData.LoadedLastTweetID = tweets[0].Id;
                        UInt64 lastId = tweets[0].Id;
                        house.AttachTweets(tweets);
                        ContextData.AttachTweets(tweets);

                        if (tlData.LoadedLastTweetID != null)
                        {
                            try
                            {
                                while (tweets.Length == 20)//If it is 20, the max count, then there may be some more tweets that not be loaded.
                                {
                                    tlData.LoadedFirstGapTweetID = tweets.Last().Id;
                                    tweets = await MainClient.RefreshAsync(aToken, tlData);
                                    house.AttachTweets(tweets);
                                    (this.DataContext as ColumnData).AttachTweets(tweets);
                                }

                            }
                            catch (Exception ex)
                            {
                                //Insert TweetGap class
                                throw ex;
                            }
                        }

                        tlData.LoadedLastTweetID = lastId;
                        tlData.LoadedFirstGapTweetID = null;
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
                if (message != null)
                    await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
            });
        }

        
    }
}
