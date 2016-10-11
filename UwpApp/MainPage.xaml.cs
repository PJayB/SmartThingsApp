using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Web.Http;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UwpApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IDisposable
    {
        HttpClient _http;
        CancellationTokenSource _cancelationTokens;


        const string c_EndPoint = "http://dev.pjblewis.com/SmartThingsApp/webui/?hello=world";

        public void Log(string s)
        {
            _debugOutput.Text += s + Environment.NewLine;
        }


        public MainPage()
        {
            this.InitializeComponent();

            _cancelationTokens = new CancellationTokenSource();
            _http = new HttpClient();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _webView.Navigate(new Uri(c_EndPoint));

            Log("Contacting " + c_EndPoint + "...");

            try
            {
                HttpResponseMessage response = await _http.GetAsync(new Uri(c_EndPoint)).AsTask(_cancelationTokens.Token);

                Log("HEADERS:");
                foreach (var i in response.Headers)
                {
                    Log($"{i.Key}: {i.Value}");
                }
                Log("CONTENT:");
                Log($"{response.Content}");
            }
            catch (TaskCanceledException)
            {
                // TODO
            }
            catch (Exception)
            {
                // TODO
            }
        }

        public void Dispose()
        {
            if (_http != null)
            {
                _http.Dispose();
                _http = null;
            }

            if (_cancelationTokens != null)
            {
                _cancelationTokens.Dispose();
                _cancelationTokens = null;
            }
        }
    }
}
