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
        Windows.UI.Xaml.DispatcherTimer timer = new Windows.UI.Xaml.DispatcherTimer() { Interval = new TimeSpan(0, 1, 0) };
        public TimelineColumn()
        {
            this.InitializeComponent();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AccountTokenCollector collector = Application.Current.Resources["AccountCollector"] as AccountTokenCollector;
            TwitterWrapper.TwitterClient MainClient = Application.Current.Resources["MainClient"] as TwitterWrapper.TwitterClient;
            TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;
            ColumnData ContextData = this.DataContext as ColumnData;

            /*
             * 관련 글 http://social.msdn.microsoft.com/Forums/en-US/winappswithcsharp/thread/312b624c-95ed-4dc5-986a-22d1a1411fe0
             * BackgroundTransfer 샘플에 아래와 똑같은 팁이 있었다 - 하나하나 await해버려서 작업이 순차적으로 되는 걸 방지
             */
            List<Task> tasks = new List<Task>();
            foreach (TwitterWrapper.ITimelineData tlData in ContextData.TimelineDatas)
            {
                tasks.Add(RefreshRespectively(collector, MainClient, house, ContextData, tlData));
            }
            await Task.WhenAll(tasks);
        }

        async Task RefreshRespectively(AccountTokenCollector collector, TwitterWrapper.TwitterClient MainClient, TweetStorage house, ColumnData ContextData, TwitterWrapper.ITimelineData tlData)
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
                    ContextData.AttachTweets(tweets);

                    if (tlData.LoadedLastTweetID != null)
                    {
                        try
                        {
                            while (tweets.Last().Id != tlData.LoadedLastTweetID)//
                            /*
                             * 처음엔 들어온 트윗들 중에 가장 마지막 트윗이 있는지 트윗 마지막부터 거꾸로 순서대로 확인하고 끊었으나 since_id는 그 트윗은 포함하지 않게 되므로 끝없이 로딩하게 됨If it is 20, the max count, then there may be some more tweets that not be loaded.
                             * If it is 20, the max count, then there may be some more tweets that not be loaded. 로 바꾸었으나, max count가 항상 안 들어와서 절망
                             * since_id를 하나 빼고 로드된 트윗 중 마지막 트윗이 타임라인 트윗 중 가장 위 트윗과 같은지 확인하고 끊기? 는 트윗 하나 더 불러오게 되어서 데이터가 조금 낭비되는데 ㅠㅠ 는 이걸로 일단 함
                             */
                            {
                                tlData.LoadedFirstGapTweetID = tweets.Last().Id - 1;
                                tweets = await MainClient.RefreshAsync(aToken, tlData);
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
        }
        
    }
}
