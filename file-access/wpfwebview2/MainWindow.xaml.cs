using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wpfwebview2
{
    public class Message
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string Info { get; set; }
    }

    public class ResultsInfo
    {
        public String Size { get; set; }
        public String Results { get; set; }
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
            String htmlPath = System.IO.Path.GetFullPath(System.IO.Path.Join(System.IO.Path.GetDirectoryName(location), "..", "..", "..", "src", "index.html"));

            webView.CoreWebView2.Navigate(htmlPath);                        
            webView.CoreWebView2.WebMessageReceived += WebMessageReceived;
        }

        void ReadFile(Message message)
        {
            var location = message.Info;
            var content = File.ReadAllText(location);

            AnswerMessage(message.Id, JsonSerializer.Serialize(content));
        }

        void ReadFolder(Message message)
        {
            var location = System.Reflection.Assembly.GetEntryAssembly().Location;
            var path = System.IO.Path.GetFullPath(System.IO.Path.Join(System.IO.Path.GetDirectoryName(location), "..", "..", "..", "..", "fixtures", message.Info));

            var files = Directory.GetFiles(path);

            AnswerMessage(message.Id, JsonSerializer.Serialize(files));
        }

        void WriteResults(Message message)
        {
            var location = System.Reflection.Assembly.GetEntryAssembly().Location;

            var resultsInfo = JsonSerializer.Deserialize<ResultsInfo>(message.Info, this.options);
            var path = System.IO.Path.GetFullPath(System.IO.Path.Join(System.IO.Path.GetDirectoryName(location), "..", "..", "..", "..", "webview-" + resultsInfo.Size + "-results.csv"));

            File.WriteAllText(path, resultsInfo.Results);

            AnswerMessage(message.Id, "ok");
        }

        void AnswerMessage(int id, string info)
        {
            var message = new Message();
            message.Id = id;
            message.Info = info;

            var serializedAnswer = JsonSerializer.Serialize<Message>(message, this.options);

            webView.CoreWebView2.PostWebMessageAsJson(serializedAnswer);
        }

        void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            String serializedMessage = args.WebMessageAsJson;
            var message = JsonSerializer.Deserialize<Message>(serializedMessage, this.options);

            switch (message.Action)
            {
                case "readFolder": ReadFolder(message); break;
                case "readFile": ReadFile(message); break;
                case "writeResults": WriteResults(message); break;
                default: break;
            }            
        }
    }
}
