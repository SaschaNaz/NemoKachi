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
    public partial class LoginVisualizer : UserControl, TwitterClient.ILoginVisualizer
    {
        public Double CurrentProcess
        {
            get { return progressBar1.Value; }
            set { progressBar1.Value = value; }
        }
        public String ErrorMessage
        {
            get { return ErrorText.Text; }
            set { ErrorText.Text = value; progressBar1.ShowError = true; }
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
