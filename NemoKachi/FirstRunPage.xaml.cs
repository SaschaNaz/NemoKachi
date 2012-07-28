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
using System.Threading.Tasks;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace NemoKachi
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class FirstRunPage : NemoKachi.Common.LayoutAwarePage
    {
        public FirstRunPage()
        {
            this.InitializeComponent();
            Login();
        }

        async void Login()
        {
            String ErrorMessage = null;

            TwitterClient client = new TwitterClient(
                (String)Application.Current.Resources["oauth_consumer_key"],
                (String)Application.Current.Resources["oauth_consumer_secret"]);
            TwitterClient.LoginHandler lhandler = new TwitterClient.LoginHandler(client, lvisual, "http://ao-k-ilapis.kr/");

            //lhandler.LoginCompleted += new TwitterClient.LoginHandler.LoginCompletedEventHandler(
            //    async delegate(Object lsender, TwitterClient.LoginHandler.LoginCompletedEventArgs le)
            //    {
            //        //Clients.Add(client);
            //        //ClientView.SelectedItem = client;
            //        //LayoutRoot.Children.Remove(visualizer);

            //        if (le.Message == TwitterClient.LoginHandler.LoginMessage.Succeed)
            //        {
            //            await new Windows.UI.Popups.MessageDialog("Login succeed").ShowAsync();
            //            //TweetPager pager = new TweetPager() { Title = client.AccountName };
            //            //pager.Columns.Add(new TweetColumnist() { Title = "Friends" });
            //            //pager.Columns.Add(new TweetColumnist() { Title = "Mentions" });
            //            //Pages.Add(pager);

            //            //await new Windows.UI.Popups.MessageDialog("MetroKachi successfully accessed your twitter account.").ShowAsync();
            //            //PageFrame.Navigate(typeof(TweetPage), pager);
            //            //this.BottomAppBar.IsOpen = true;
            //            //로그인 된 계정용의 새 페이지를 추가해서, 이벤트 뜨면 새 페이지로 바로 Navigate 하도록 함
            //        }
            //        else if (le.Message == TwitterClient.LoginHandler.LoginMessage.UserDenied)
            //        {
            //            await new Windows.UI.Popups.MessageDialog("Login process is cancelled by you.").ShowAsync();
            //        }
            //    });
            //visualizer.Closed += new RoutedEventHandler(delegate
            //{
            //    LayoutRoot.Children.Remove(visualizer);
            //});

            try
            {
                TwitterClient.AccountInfo loginArgs = await lhandler.AccountLoginAsync();
                await new Windows.UI.Popups.MessageDialog("Login succeed").ShowAsync();
                ((Application.Current.Resources["accountsCollection"] as CollectionViewSource).Source as List<TwitterClient>).Add(client);
                Frame.Navigate(typeof(MainPage));
                //if (loginTask.IsCompleted)
                //{
                //        //TweetPager pager = new TweetPager() { Title = client.AccountName };
                //        //pager.Columns.Add(new TweetColumnist() { Title = "Friends" });
                //        //pager.Columns.Add(new TweetColumnist() { Title = "Mentions" });
                //        //Pages.Add(pager);

                //        //await new Windows.UI.Popups.MessageDialog("MetroKachi successfully accessed your twitter account.").ShowAsync();
                //        //PageFrame.Navigate(typeof(TweetPage), pager);
                //        //this.BottomAppBar.IsOpen = true;
                //        //로그인 된 계정용의 새 페이지를 추가해서, 이벤트 뜨면 새 페이지로 바로 Navigate 하도록 함
                //}
                //else if (loginTask.IsCanceled)
                //{
                //    await new Windows.UI.Popups.MessageDialog("You canceled the process").ShowAsync();
                //}
                //else if (loginTask.IsFaulted)
                //{
                //    await new Windows.UI.Popups.MessageDialog("There were some problems.").ShowAsync();
                //}
            }
            catch (System.Net.Http.HttpRequestException)
            {
                ErrorMessage = "The app cannot connect to the internet. Please check your connection.";
            }
            catch (TaskCanceledException)
            {
                ErrorMessage = "You canceled the process";
            }
            if (ErrorMessage != null)
            {
                //LayoutRoot.Children.Remove(visualizer);
                await new Windows.UI.Popups.MessageDialog(ErrorMessage).ShowAsync();
            }
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
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }
    }
}
