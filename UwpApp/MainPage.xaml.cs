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

        private void ExecuteOnUiThread(Action a)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                a();
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public void Log(string s)
        {
            ExecuteOnUiThread(() => 
            { 
                _debugOutput.Text += s + Environment.NewLine;
            });
        }

        public MainPage()
        {
            this.InitializeComponent();

            _cancelationTokens = new CancellationTokenSource();

            //
            // Disable caching! 
            //

            var httpFilter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
            httpFilter.CacheControl.ReadBehavior = Windows.Web.Http.Filters.HttpCacheReadBehavior.NoCache;
            _http = new HttpClient(httpFilter);
        }

        private void LogHttpResponse(HttpResponseMessage response)
        {
            Log("Response: " + response.StatusCode);
            Log("HEADERS:");
            foreach (var i in response.Headers)
            {
                Log($"{i.Key}: {i.Value}");
            }
            Log("CONTENT:");
            Log($"{response.Content}");
        }

        private async void HttpRequestCompleted(IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> op, AsyncStatus status)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                if (status == AsyncStatus.Canceled)
                {
                    Log("Canceled.");
                }
                else
                {
                    LogHttpResponse(op.GetResults());
                }
            });

            ExecuteOnUiThread(() =>
            {
                _progressBar.IsIndeterminate = false;
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
            public string Uri;
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

                ep.Uri = strings[0];
                ep.AuthToken = strings[1];

                if (!ep.Uri.EndsWith("/"))
                    ep.Uri += "/";

                return ep;
            } 
            catch (Exception ex)
            {
                Log("Couldn't read endpoint data from file: " + ex.Message);
                Log("Path: " + Windows.Storage.ApplicationData.Current.LocalFolder.Path);
                return null;
            }
        }

        HttpRequestMessage ConstructMessage(SmartThingsEndPoint endPoint, string command = null, HttpMethod method = null)
        {
            Uri uri = new Uri(endPoint.Uri + command ?? "");

            var message = new HttpRequestMessage()
            {
                RequestUri = uri,
                Method = method ?? HttpMethod.Get
            };

            message.Headers.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Bearer", endPoint.AuthToken);

            return message;
        }

        async Task<HttpResponseMessage> GetAllSwitches(SmartThingsEndPoint endPoint)
        {
            var message = ConstructMessage(endPoint);

            Log("Contacting " + message.RequestUri + "...");

            IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> httpOperation = _http.SendRequestAsync(message);
            //httpOperation.Completed = new AsyncOperationWithProgressCompletedHandler<HttpResponseMessage, HttpProgress>(HttpRequestCompleted);
            httpOperation.Progress = new AsyncOperationProgressHandler<HttpResponseMessage, HttpProgress>(HttpRequestProgress);
            return await httpOperation;
        }

        async Task<HttpResponseMessage> TurnOnAllSwitches(SmartThingsEndPoint endPoint)
        {
            var message = ConstructMessage(endPoint, "on", HttpMethod.Put);

            Log("Contacting " + message.RequestUri + "...");

            IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> httpOperation = _http.SendRequestAsync(message);
            //httpOperation.Completed = new AsyncOperationWithProgressCompletedHandler<HttpResponseMessage, HttpProgress>(HttpRequestCompleted);
            httpOperation.Progress = new AsyncOperationProgressHandler<HttpResponseMessage, HttpProgress>(HttpRequestProgress);
            return await httpOperation;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _webView.Navigate(new Uri(c_DisplayEndPoint));

            _progressBar.IsIndeterminate = true;

            try
            {
                SmartThingsEndPoint endPoint = await ReadEndPointFromFileAsync();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                var task = new Task(async () => 
                {
                    var getResult = await GetAllSwitches(endPoint);
                    LogHttpResponse(getResult);
                    var setResult = await TurnOnAllSwitches(endPoint);
                    LogHttpResponse(setResult);

                    // A set and get back-to-back seems to return stale data, so wait a bit :/
                    await Task.Delay(500);

                    var getResult2 = await GetAllSwitches(endPoint);
                    LogHttpResponse(getResult2);

                    ExecuteOnUiThread(() =>
                    {
                        _progressBar.IsIndeterminate = false;
                    });
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                task.Start();
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
