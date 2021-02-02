using System;
using System.IO;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using System.Windows;

namespace ipc_wv2
{

    public class Message
    {
        public long id { get; set; }
        public long start { get; set; }
        public long duration { get; set; }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);

            String location = System.Reflection.Assembly.GetEntryAssembly().Location;

            /*
             * C:\\Users\\User\\source\\repos\\wpfwebview2\\bin\\Debug\\netcoreapp3.1\\wpfwebview2.dll"
             * C:\\Users\\User\\source\\repos\\wpfwebview2\\src\\index.html
             */
            String htmlPath = Path.GetFullPath(Path.Join(Path.GetDirectoryName(location), "..", "..", "..", "web", "renderer.html"));

            webView.CoreWebView2.Navigate(htmlPath);
            webView.CoreWebView2.WebMessageReceived += WebMessageReceived;
        }

        void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            String serializedMessage = args.WebMessageAsJson;

            var message = JsonSerializer.Deserialize<Message>(serializedMessage, this.options);
            var serializedAnswer = JsonSerializer.Serialize<Message>(message, this.options);

            // If we just send the string we receive the results are still in the same order of magnitude
            //String serializedAnswer = serializedMessage;
                        
            webView.CoreWebView2.PostWebMessageAsJson(serializedAnswer);
        }
    }
}
