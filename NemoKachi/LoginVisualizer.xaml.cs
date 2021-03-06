﻿using System;
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

            if (!typeof(LoginPhase).Equals(value.GetType()))
                throw new ArgumentException("Value must be of type (TwitterClient.LoginPhase).", "value");

            LoginPhase phase = (LoginPhase)value;
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            switch (phase)
            {
                case LoginPhase.WaitingOAuthCallback:
                    return loader.GetString("WaitingOAuthCallback");
                case LoginPhase.AuthorizingApp:
                    return loader.GetString("AuthorizingApp");
                case LoginPhase.VerifyingTempToken:
                    return loader.GetString("VerifyingTempToken");
                case LoginPhase.AccessingToken:
                    return loader.GetString("AccessingToken");
                case LoginPhase.LoadingAccountInformation:
                    return loader.GetString("LoadingAccountInformation");
                case LoginPhase.GettingAccountImageURI:
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

    public partial class LoginVisualizer : UserControl, ILoginVisualizer
    {
        public static readonly DependencyProperty PhaseProperty =
            DependencyProperty.Register(
            "Phase",
            typeof(LoginPhase),
            typeof(LoginVisualizer), new PropertyMetadata(LoginPhase.WaitingOAuthCallback));

        public LoginPhase Phase
        {
            get { return (LoginPhase)GetValue(PhaseProperty); }
            set { SetValue(PhaseProperty, (LoginPhase)value); }
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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OnClosed(e);
        }
    }
}
