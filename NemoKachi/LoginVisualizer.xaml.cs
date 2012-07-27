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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NemoKachi
{
    public class LoginphaseStringConverter : Windows.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value == null)
                throw new ArgumentNullException("value", "Value cannot be null.");

            if (!typeof(TwitterClient.LoginPhase).Equals(value.GetType()))
                throw new ArgumentException("Value must be of type (TwitterClient.LoginPhase).", "value");

            TwitterClient.LoginPhase phase = (TwitterClient.LoginPhase)value;
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            switch (phase)
            {
                case TwitterClient.LoginPhase.RecievingOAuthCallback:
                    return loader.GetString("RecievingOAuthCallback");
                case TwitterClient.LoginPhase.AuthorizingApp:
                    return loader.GetString("AuthorizingApp");
                case TwitterClient.LoginPhase.VerifyingTempToken:
                    return loader.GetString("VerifyingTempToken");
                case TwitterClient.LoginPhase.AccessingToken:
                    return loader.GetString("AccessingToken");
                case TwitterClient.LoginPhase.LoadingAccountInformation:
                    return loader.GetString("LoadingAccountInformation");
                case TwitterClient.LoginPhase.AccessingAccountImageURI:
                    return loader.GetString("AccessingAccountImageURI");
                case TwitterClient.LoginPhase.LoadingAccountImage:
                    return loader.GetString("LoadingAccountImage");
                default:
                    return loader.GetString("(Error)");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class LoginVisualizer : UserControl, TwitterClient.ILoginVisualizer
    {
        public TwitterClient.LoginPhase Phase { get; set; }
        //public String CurrentMessage
        //{
        //    get { return messageBlock.Text; }
        //    set { messageBlock.Text = value; }
        //}

        public event RoutedEventHandler Closed;
        protected virtual void OnClosed(RoutedEventArgs e)
        {
            if (Closed != null)
            {
                Closed(this, e);
            }
        }

        public LoginVisualizer()
        {
            this.InitializeComponent();
            DataContext = this;
        }

        public WebView SetWebView(Uri AuthUri)
        {
            webviewGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
            return webView1;
        }

        public void RemoveWebView()
        {
            webviewGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OnClosed(e);
        }
    }
}
