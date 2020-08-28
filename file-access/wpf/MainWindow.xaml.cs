using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly string root = Path.GetFullPath(Path.Join(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "..", "..", "..", ".."));
        readonly int RERUNS = 5;
        readonly int PARALLEL_WORKERS = 100;

        public MainWindow()
        {
            InitializeComponent();
        }

        async Task<string> ReadFile(String filePath)
        {
            // The Async version blocks the UI thread: https://stackoverflow.com/questions/63217657/why-file-readalllinesasync-blocks-the-ui-thread
            var content = await Task.Run(() => File.ReadAllText(filePath));

            return content;
        }

        async Task<(int, long)> ReadSequentially(string[] files)
        {
            var sw = new Stopwatch();

            sw.Start();

            foreach (var file in files)
            {
                await ReadFile(file);
            }

            sw.Stop();

            return (files.Length, sw.ElapsedMilliseconds);
        }

        (string[], long) ReadFolderContent(String folderName)
        {
            var sw = new Stopwatch();
            sw.Start();
            var path = Path.GetFullPath(Path.Join(this.root, "fixtures", folderName));

            var files = Directory.GetFiles(path);

            sw.Stop();
            return (files, sw.ElapsedMilliseconds);
        }

        async Task ReadParallel(List<string> files)
        {
            if(files.Count == 0)
            {
                return;
            }

            var filePath = files[0];
            files.RemoveAt(0);

            await ReadFile(filePath);
            
            await ReadParallel(files);
        }

        async Task<(int, long)> ReadInParallel(string[] files)
        {
            var sw = new Stopwatch();
            sw.Start();

            /* Using mutex to limit how many are running simultaneously */
            //var mutex = new SemaphoreSlim(PARALLEL_WORKERS);
            //var tasks = Enumerable.Range(0, files.Length).Select(async item =>
            //{
            //    await mutex.WaitAsync();
            //    try { await ReadFile(files[item]); }
            //    finally { mutex.Release(); }
            //});
            //await Task.WhenAll(tasks);

            /* Creating workers that call themselves recursively until all files are read */
            var workers = new List<Task>();
            var filesList = new List<string>(files);
            for (int i = 0; i < this.PARALLEL_WORKERS; i++)
            {
                workers.Add(ReadParallel(filesList));
            }
            await Task.WhenAll(workers.ToArray());

            /* Reading all files directly --> Crashes everything */
            //List<Task> tasks = new List<Task>();
            //foreach (var filePath in files)
            //{
            //    Task task = Task.Run(() => File.ReadAllText(filePath));
            //    tasks.Add(task);
            //}
            //await Task.WhenAll(tasks.ToArray());

            sw.Stop();

            return (files.Length, sw.ElapsedMilliseconds);
        }

        void WriteResults(String size, String results)
        {
            var path = Path.GetFullPath(Path.Join(this.root, "wpf-" + size + "-results.csv"));

            File.WriteAllText(path, results);
        }

        long[] CalculateAverage(long[][] results)
        {
            int testsLength = results[0].Length;
            long[] aggregation = new long[testsLength];

            foreach (var result in results)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    aggregation[i] += result[i];
                }
            };

            for (int i = 0; i < aggregation.Length; i++)
            {
                aggregation[i] = aggregation[i] / results.Length;
            }

            return aggregation;
        }

        async Task<long[]> Benchmark(string size)
        {
            var (files, timeToList) = ReadFolderContent(size);

            var (filesLength, seqTime) = await ReadSequentially(files);
            // For some reason parallel still blocks the UI at some points 🤔
            var (filesLength2, parallelTime) = await ReadInParallel(files);

            return new long[] { timeToList, seqTime, parallelTime };
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            var fileSizes = new List<string>(new string[] { "4k", "1mb" });

            Start.IsEnabled = false;

            while (fileSizes.Count > 0)
            {
                var size = fileSizes[0];
                fileSizes.RemoveAt(0);

                var results = new List<long[]>();

                for (int i = 0; i < this.RERUNS; i++)
                {
                    Status.Text = $"Running benchmark for size {size} ({i})";

                    var result = await Benchmark(size);

                    results.Add(result);
                }

                var average = CalculateAverage(results.ToArray());
                var timeToList = average[0];
                var seqTime = average[1];
                var parallelTime = average[2];

                var stringResults = $@"Action,Time elapsed(ms)
Read dir,{timeToList}
Sequential read,{seqTime}
Parallel read,{parallelTime}";

                WriteResults(size, stringResults);
            }

            Start.IsEnabled = false;

            Status.Text = "Done";
        }
    }
}
