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
            TwitterClient.LoginHandler lhandler = new TwitterClient.LoginHandler(Application.Current.Resources["MainClient"] as TwitterWrapper.TwitterClient, lvisual, "http://ao-k-ilapis.kr/");
            AccountToken loginArgs = null;
            while (loginArgs == null)
            {
                String ErrorMessage = null;
                try
                {
                    loginArgs = await lhandler.AccountLoginAsync();
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    ErrorMessage = "Our magpie cannot dive into the internet. Please check your connection.";
                }
                catch (TaskCanceledException)
                {
                    ErrorMessage = "Was there some problem in authorizing? Our magpie will restart authorizing for you right now. Sorry for inconvinience.";
                }
                if (ErrorMessage != null)
                {
                    //LayoutRoot.Children.Remove(visualizer);
                    await new Windows.UI.Popups.MessageDialog(ErrorMessage).ShowAsync();
                }
            }

            await new Windows.UI.Popups.MessageDialog("Login succeed").ShowAsync();
            (Application.Current.Resources["AccountCollector"] as AccountTokenCollector).TokenCollection.Add(loginArgs);
            ITimelineData tlData = new FollowingTweetsData(loginArgs.AccountId, new LocalRefreshRequest());
            ColumnData colData = new ColumnData(tlData);
            #region temp
            Application.Current.Resources.Add("tempColumn", colData);
            #endregion

            Frame.Navigate(typeof(MainPage));
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
