using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly static string ROOT = Path.GetFullPath(Path.Join(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "..", "..", "..", ".."));
        readonly static string FIXTURES_PATH = Path.GetFullPath(Path.Join(ROOT, "fixtures"));
        readonly static string SOURCES_PATH = Path.GetFullPath(Path.Join(ROOT, "source-files"));
        readonly int RERUNS = 5;
        readonly int PARALLEL_WORKERS = 100;
        readonly int FILES = 10000;

        public MainWindow()
        {
            InitializeComponent();
        }

        async Task<string> ReadFileAsync(String path)
        {
            // The Async version blocks the UI thread: https://stackoverflow.com/questions/63217657/why-file-readalllinesasync-blocks-the-ui-thread
            var content = await Task.Run(() => File.ReadAllText(path));

            return content;
        }

        async Task WriteFileAsync(String path, String content)
        {
            await Task.Run(() => File.WriteAllText(path, content));
        }

        async Task CreateDirectoryAsync(String path)
        {
            await Task.Run(() => Directory.CreateDirectory(path));
        } 

        async Task<(int, long)> ReadSequentially(string[] files)
        {
            var sw = new Stopwatch();

            sw.Start();

            foreach (var file in files)
            {
                await ReadFileAsync(file);
            }

            sw.Stop();

            return (files.Length, sw.ElapsedMilliseconds);
        }

        (string[], long) ReadFolderContent(String folderName)
        {
            var sw = new Stopwatch();
            sw.Start();
            var path = Path.GetFullPath(Path.Join(FIXTURES_PATH, folderName));

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

            await ReadFileAsync(filePath);
            
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

        async Task WriteParallel(List<string> files, String content)
        {
            if (files.Count == 0)
            {
                return;
            }

            var filePath = files[0];
            files.RemoveAt(0);

            await WriteFileAsync(filePath, content);

            await WriteParallel(files, content);
        }


        async Task<(int, long)> WriteConcurrently(String size)
        {
            var sw = new Stopwatch();
            sw.Start();

            var targetFolder = Path.GetFullPath(Path.Join(FIXTURES_PATH, size)); ;

            await CreateDirectoryAsync(targetFolder);
            

            /* Creating workers that call themselves recursively until all files are read */
            var workers = new List<Task>();
            var filesList = new List<string>();
            
            for(var i =0; i< FILES; i++)
            {
                filesList.Add(Path.Join(targetFolder, $"{size}-{i}.txt"));
            }
                        
            var content = await ReadFileAsync(Path.GetFullPath(Path.Join(SOURCES_PATH, $"{size}.txt")));
            
            
            for (int i = 0; i < this.PARALLEL_WORKERS; i++)
            {
                workers.Add(WriteParallel(filesList, content));
            }
            await Task.WhenAll(workers.ToArray());

            sw.Stop();

            return (filesList.Count, sw.ElapsedMilliseconds);
        }
                
        async Task DeleteFixtures()
        {
            try
            {
                await Task.Run(() => Directory.Delete(FIXTURES_PATH, true));
            }
            catch(Exception e) {
                Console.WriteLine("The process failed: {0}", e.Message);
            }

            await Task.Run(() => Directory.CreateDirectory(FIXTURES_PATH));
        }

        void WriteResults(String size, String results)
        {
            var path = Path.GetFullPath(Path.Join(ROOT, "wpf-" + size + "-results.csv"));

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
            await DeleteFixtures();

            var (writtenFiles, timeToWrite) = await WriteConcurrently(size);

            var (files, timeToList) = ReadFolderContent(size);

            var (filesLength, seqTime) = await ReadSequentially(files);
            // For some reason parallel still blocks the UI at some points 🤔
            var (filesLength2, parallelTime) = await ReadInParallel(files);

            return new long[] { timeToWrite, timeToList, seqTime, parallelTime };
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
                var timeToWrite = average[0];
                var timeToList = average[1];
                var seqTime = average[2];
                var parallelTime = average[3];

                var stringResults = $@"Action,Time elapsed(ms)
Write files,{timeToWrite}
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
