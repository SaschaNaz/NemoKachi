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
                case TwitterClient.LoginPhase.WaitingOAuthCallback:
                    return loader.GetString("WaitingOAuthCallback");
                case TwitterClient.LoginPhase.AuthorizingApp:
                    return loader.GetString("AuthorizingApp");
                case TwitterClient.LoginPhase.VerifyingTempToken:
                    return loader.GetString("VerifyingTempToken");
                case TwitterClient.LoginPhase.AccessingToken:
                    return loader.GetString("AccessingToken");
                case TwitterClient.LoginPhase.LoadingAccountInformation:
                    return loader.GetString("LoadingAccountInformation");
                case TwitterClient.LoginPhase.GettingAccountImageURI:
                    return loader.GetString("GettingAccountImageURI");
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
        public static readonly DependencyProperty PhaseProperty =
            DependencyProperty.Register(
            "Phase",
            typeof(TwitterClient.LoginPhase),
            typeof(LoginVisualizer), new PropertyMetadata(TwitterClient.LoginPhase.WaitingOAuthCallback));

        public TwitterClient.LoginPhase Phase
        {
            get { return (TwitterClient.LoginPhase)GetValue(PhaseProperty); }
            set { SetValue(PhaseProperty, (TwitterClient.LoginPhase)value); }
        }

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
            Closed += LoginVisualizer_Closed;
        }

        void LoginVisualizer_Closed(object sender, RoutedEventArgs e)
        {
            RemoveWebView();
        }

        public WebView GetWebView()
        {
            return webView1;
        }

        public Boolean IsWebViewSet { get; private set; }

        public void SetWebView()
        {
            IsWebViewSet = true;
            WebViewStart.Begin();
            //webviewGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        public void RemoveWebView()
        {
            IsWebViewSet = false;
            WebViewEnd.Begin();
            //WebViewStoryboard.
            //webviewGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OnClosed(e);
        }
    }
}
