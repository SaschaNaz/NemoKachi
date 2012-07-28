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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NemoKachi
{
    public sealed partial class TweetInput : UserControl
    {
        public CollectionViewSource cvs { get; set; }//아나 역시 커스텀 컬렉션 만들어야되나!

        public TweetInput()
        {
            this.InitializeComponent();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            String sendText;
            SendTextBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out sendText);
            await ((cvs as CollectionViewSource).Source as List<TwitterWrapper.TwitterClient>)[0].SendTweet(
                new TwitterWrapper.TwitterClient.SendTweetQuery()
                {
                   status = sendText
                });
        }
    }
}
