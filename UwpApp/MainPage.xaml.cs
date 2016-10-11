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

namespace UwpApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IDisposable
    {
        HttpClient _http;
        CancellationTokenSource _cancelationTokens;

        const string c_DisplayEndPoint = "http://dev.pjblewis.com/SmartThingsApp/webui/?hello=world";

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

        private async void HttpRequestCompleted(IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> op, AsyncStatus status)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                if (status == AsyncStatus.Canceled)
                {
                    Log("Canceled.");
                }
                else if (status == AsyncStatus.Error)
                {
                    Log("Error: " + op.ErrorCode.Message);
                }
                else if (status == AsyncStatus.Completed)
                {
                    var response = op.GetResults();

                    Log("HEADERS:");
                    foreach (var i in response.Headers)
                    {
                        Log($"{i.Key}: {i.Value}");
                    }
                    Log("CONTENT:");
                    Log($"{response.Content}");

                    _progressBar.IsIndeterminate = false;
                }
            });
        }

        private async void HttpRequestProgress(IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> op, HttpProgress progress)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => 
            {
                string totalBytes = progress.TotalBytesToReceive.HasValue ? "/" + progress.TotalBytesToReceive.Value.ToString() : string.Empty;
                Log($"Recieved {progress.BytesReceived}{totalBytes} bytes...");
            });
        }


        class SmartThingsEndPoint
        {
            public Uri Uri;
            public string AuthToken;
        }

        async Task<SmartThingsEndPoint> ReadEndPointFromFileAsync()
        {
            try
            {
                SmartThingsEndPoint ep = new SmartThingsEndPoint();

                Windows.Storage.StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile file = await folder.GetFileAsync("endpoint.txt");

                var strings = await Windows.Storage.FileIO.ReadLinesAsync(file);

                ep.Uri = new Uri(strings[0]);
                ep.AuthToken = strings[1];

                return ep;
            } 
            catch (Exception ex)
            {
                Log("Couldn't read endpoint data from file: " + ex.Message);
                Log("Path: " + Windows.Storage.ApplicationData.Current.LocalFolder.Path);
                return null;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _webView.Navigate(new Uri(c_DisplayEndPoint));

            _progressBar.IsIndeterminate = true;

            try
            {
                SmartThingsEndPoint endPoint = await ReadEndPointFromFileAsync();

                Log("Contacting " + endPoint.Uri.AbsoluteUri + "...");

                var message = new HttpRequestMessage()
                {
                    RequestUri = endPoint.Uri,
                    Method = HttpMethod.Get
                };

                message.Headers.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Bearer", endPoint.AuthToken);

                IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> httpOperation = _http.SendRequestAsync(message);
                httpOperation.Completed = new AsyncOperationWithProgressCompletedHandler<HttpResponseMessage, HttpProgress>(HttpRequestCompleted);
                httpOperation.Progress = new AsyncOperationProgressHandler<HttpResponseMessage, HttpProgress>(HttpRequestProgress);
                httpOperation.AsTask().Start();
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
