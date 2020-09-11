using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;


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

    public class FileInfo
    {
        public String Path { get; set; }
        public String Content { get; set; }
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

        readonly static string ROOT = Path.GetFullPath(Path.Join(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "..", "..", "..", ".."));
        readonly static string FIXTURES_PATH = Path.GetFullPath(Path.Join(ROOT, "fixtures"));
        readonly static string SOURCES_PATH = Path.GetFullPath(Path.Join(ROOT, "source-files"));

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);

            String location = System.Reflection.Assembly.GetEntryAssembly().Location;

            /*
             * C:\\Users\\User\\source\\repos\\wpfwebview2\\bin\\Debug\\netcoreapp3.1\\wpfwebview2.dll"
             * C:\\Users\\User\\source\\repos\\wpfwebview2\\src\\index.html
             */
            String htmlPath = Path.GetFullPath(Path.Join(Path.GetDirectoryName(location), "..", "..", "..", "src", "index.html"));

            webView.CoreWebView2.Navigate(htmlPath);
            webView.CoreWebView2.WebMessageReceived += WebMessageReceived;
        }

        async Task ReadFile(Message message)
        {           
            var location = message.Info.StartsWith("C:") ?
                message.Info :
                Path.GetFullPath(Path.Join(ROOT, message.Info));
            var content = await Task.Run(() => File.ReadAllText(location));

            AnswerMessage(message.Id, JsonSerializer.Serialize(content));
        }

        void ReadFolder(Message message)
        {
            var location = System.Reflection.Assembly.GetEntryAssembly().Location;
            var path = Path.GetFullPath(Path.Join(Path.GetDirectoryName(location), "..", "..", "..", "..", "fixtures", message.Info));

            var files = Directory.GetFiles(path);

            AnswerMessage(message.Id, JsonSerializer.Serialize(files));
        }

        async Task WriteFile(Message message)
        {
            var resultsInfo = JsonSerializer.Deserialize<FileInfo>(message.Info, this.options);
            var path = Path.GetFullPath(Path.Join(ROOT, resultsInfo.Path));

            await Task.Run(() => File.WriteAllText(path, resultsInfo.Content));

            AnswerMessage(message.Id, "ok");
        }

        async Task DeleteFixtures(Message message)
        {
            try
            {
                await Task.Run(() => Directory.Delete(FIXTURES_PATH, true));
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.Message);
            }

            await Task.Run(() => Directory.CreateDirectory(FIXTURES_PATH));

            AnswerMessage(message.Id, "ok");
        }

        async Task CreateFolder(Message message)
        {   
            var path = Path.GetFullPath(Path.Join(ROOT, message.Info));

            await Task.Run(() => Directory.CreateDirectory(path));

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

        void WriteResults(Message message)
        {
            var location = System.Reflection.Assembly.GetEntryAssembly().Location;

            var resultsInfo = JsonSerializer.Deserialize<ResultsInfo>(message.Info, this.options);
            var path = Path.GetFullPath(Path.Join(Path.GetDirectoryName(location), "..", "..", "..", "..", "webview-" + resultsInfo.Size + "-results.csv"));

            File.WriteAllText(path, resultsInfo.Results);

            AnswerMessage(message.Id, "ok");
        }

        void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            String serializedMessage = args.WebMessageAsJson;
            var message = JsonSerializer.Deserialize<Message>(serializedMessage, this.options);

            switch (message.Action)
            {
                case "createFolder": CreateFolder(message); break;
                case "deleteFixtures": DeleteFixtures(message); break;
                case "readFolder": ReadFolder(message); break;
                case "readFile": ReadFile(message); break;
                case "writeFile": WriteFile(message); break;
                case "writeResults": WriteResults(message); break;
                default: break;
            }            
        }
    }
}
