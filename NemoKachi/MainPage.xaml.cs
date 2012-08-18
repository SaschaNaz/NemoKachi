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
using NemoKachi.TwitterWrapper;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace NemoKachi
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MainPage : NemoKachi.Common.LayoutAwarePage
    {
        public MainPage()
        {
            this.InitializeComponent();
            //new TwitterClient(
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            //// Restore values stored in session state.
            //if (pageState != null)
            //{
            //    if (pageState.ContainsKey("isLoginSucceed"))
            //        if ((Boolean)pageState["isLoginSucceed"] == true)
            //            greetingOutput.Text = "Login Succeed";
            //}

            //Windows.Storage.ApplicationDataContainer localSettings =
            //    Windows.Storage.ApplicationData.Current.LocalSettings;
            //if (localSettings.Values.ContainsKey("userName"))
            //{
            //    nameInput.Text = localSettings.Values["userName"].ToString();
            //}

        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            //pageState["isLoginSucceed"] = true;
        }

        private async void IdLoad(object sender, RoutedEventArgs e)
        {
            AccountTokenCollector collector = Application.Current.Resources["AccountCollector"] as AccountTokenCollector;
            TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;

            String message = null;
            UInt64 Id = Convert.ToUInt64(idBox.Text);
            try
            {
                message = (await house.LoadTweet(collector.TokenCollection[0], Id)).Text;
            }
            catch (TweetStorage.TweetStorageLockedException)
            {
                message = "You locked this tweet. Do you want to unlock it?";
            }
            catch (Exception ex)
            {
                message = String.Format("We couldn't get the tweet you requested. Sorry for that.\r\nMessage: {0}\r\nId: {1}", ex.Message, Id);
            }
            if (message != null)
                await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
        }

        private async void RegisterReplyIdTemporary(object sender, RoutedEventArgs e)
        {
            String message = null;
            try
            {
                AccountTokenCollector collector = Application.Current.Resources["AccountCollector"] as AccountTokenCollector;
                TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;
                String inputText = replyIdBox.Text;
                if (inputText != "")
                {
                    tweetInput.ReplyTweet = await house.LoadTweet(collector.TokenCollection[0], Convert.ToUInt64(inputText));
                }
                else
                    tweetInput.ReplyTweet = null;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            if (message != null)
                await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
        }

        private async void RetweetTemporary(object sender, RoutedEventArgs e)
        {
            String message = null;
            try
            {
                AccountTokenCollector collector = Application.Current.Resources["AccountCollector"] as AccountTokenCollector;
                TwitterClient MainClient = Application.Current.Resources["MainClient"] as TwitterClient;
                TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;
                String inputText = retweetIdBox.Text;
                if (inputText != "")
                {
                    TwitterWrapper.TwitterDatas.Tweet targetTweet = await house.LoadTweet(collector.TokenCollection[0], Convert.ToUInt64(inputText));

                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(String.Format("Do you want to retweet this?\r\n{0}", targetTweet.Text));
                    dialog.Commands.Add(new Windows.UI.Popups.UICommand("Retweet", null, "Yes"));
                    dialog.Commands.Add(new Windows.UI.Popups.UICommand("Cancel", null, "No"));
                    dialog.CancelCommandIndex = 1;
                    String resultId = (String)(await dialog.ShowAsync()).Id;
                    if (resultId == "Yes")
                    {
                        await MainClient.StatusRetweetAsync(collector.TokenCollection[0], targetTweet.Id, null);
                        await new Windows.UI.Popups.MessageDialog("Retweet Succeed").ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            if (message != null)
                await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
            //var abc = await  {   }.ShowAsync();
        }

        private async void DestroyTemporary(object sender, RoutedEventArgs e)
        {
            String message = null;
            try
            {
                AccountTokenCollector collector = Application.Current.Resources["AccountCollector"] as AccountTokenCollector;
                TwitterClient MainClient = Application.Current.Resources["MainClient"] as TwitterClient;
                TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;
                String inputText = destroyIdBox.Text;
                if (inputText != "")
                {
                    TwitterWrapper.TwitterDatas.Tweet targetTweet = await house.LoadTweet(collector.TokenCollection[0], Convert.ToUInt64(inputText));

                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(String.Format("Do you want to destroy this?\r\n{0}", targetTweet.Text));
                    dialog.Commands.Add(new Windows.UI.Popups.UICommand("Destroy", null, "Yes"));
                    dialog.Commands.Add(new Windows.UI.Popups.UICommand("Cancel", null, "No"));
                    dialog.CancelCommandIndex = 1;
                    String resultId = (String)(await dialog.ShowAsync()).Id;
                    if (resultId == "Yes")
                    {
                        await MainClient.StatusDestroyAsync(collector.TokenCollection[0], targetTweet.Id, null);
                        await new Windows.UI.Popups.MessageDialog("Destroy Succeed").ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            if (message != null)
                await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
        }
        
        private async void FavoriteTemporary(object sender, RoutedEventArgs e)
        {
            String message = null;
            try
            {
                AccountTokenCollector collector = Application.Current.Resources["AccountCollector"] as AccountTokenCollector;
                TwitterClient MainClient = Application.Current.Resources["MainClient"] as TwitterClient;
                TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;
                String inputText = favoriteIdBox.Text;
                if (inputText != "")
                {
                    TwitterWrapper.TwitterDatas.Tweet targetTweet = await house.LoadTweet(collector.TokenCollection[0], Convert.ToUInt64(inputText));

                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(String.Format("Do you want to favorite this?\r\n{0}", targetTweet.Text));
                    dialog.Commands.Add(new Windows.UI.Popups.UICommand("Favorite", null, "Yes"));
                    dialog.Commands.Add(new Windows.UI.Popups.UICommand("Cancel", null, "No"));
                    dialog.CancelCommandIndex = 1;
                    String resultId = (String)(await dialog.ShowAsync()).Id;
                    if (resultId == "Yes")
                    {
                        await MainClient.FavoriteCreateAsync(collector.TokenCollection[0], targetTweet.Id, null);
                        await new Windows.UI.Popups.MessageDialog("Favorite Succeed").ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            if (message != null)
                await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
        }

        private async void FavoriteDestroyTemporary(object sender, RoutedEventArgs e)
        {
            String message = null;
            try
            {
                AccountTokenCollector collector = Application.Current.Resources["AccountCollector"] as AccountTokenCollector;
                TwitterClient MainClient = Application.Current.Resources["MainClient"] as TwitterClient;
                TweetStorage house = Application.Current.Resources["TweetHouse"] as TweetStorage;
                String inputText = unfavoriteIdBox.Text;
                if (inputText != "")
                {
                    TwitterWrapper.TwitterDatas.Tweet targetTweet = await house.LoadTweet(collector.TokenCollection[0], Convert.ToUInt64(inputText));

                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(String.Format("Do you want to unfavorite this?\r\n{0}", targetTweet.Text));
                    dialog.Commands.Add(new Windows.UI.Popups.UICommand("Unfavorite", null, "Yes"));
                    dialog.Commands.Add(new Windows.UI.Popups.UICommand("Cancel", null, "No"));
                    dialog.CancelCommandIndex = 1;
                    String resultId = (String)(await dialog.ShowAsync()).Id;
                    if (resultId == "Yes")
                    {
                        await MainClient.FavoriteDestroyAsync(collector.TokenCollection[0], targetTweet.Id, null);
                        await new Windows.UI.Popups.MessageDialog("Unfavorite Succeed").ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            if (message != null)
                await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
        }

        //private void TweetIDRecieved(object sender, RoutedEventArgs e)
        //{
        //    Windows.Storage.ApplicationDataContainer localSettings =
        //        Windows.Storage.ApplicationData.Current.LocalSettings;
        //    localSettings.Values["userName"] = nameInput.Text;

        //    greetingOutput.Text = "Login Succeed";
        //}
    }
}
