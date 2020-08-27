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
using System.Windows.Shapes;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly string root = System.IO.Path.GetFullPath(System.IO.Path.Join(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "..", "..", "..", ".."));
        readonly int RERUNS = 5;

        public MainWindow()
        {
            InitializeComponent();
        }

        async Task<string> ReadFile(String filePath)
        {
            var content = await File.ReadAllTextAsync(filePath);

            return content;
        }

        async Task<(int,long)> ReadSequentially (string[] files)
        {
            var sw = new Stopwatch();

            sw.Start();

            for (int i = 0; i < files.Length; i++)
            {
                await ReadFile(files[i]);
            }

            sw.Stop();
            
            return (files.Length, sw.ElapsedMilliseconds);
        }

        (string[], long) ReadFolderContent(String folderName)
        {
            var sw = new Stopwatch();
            sw.Start();
            var path = System.IO.Path.GetFullPath(System.IO.Path.Join(this.root, "fixtures", folderName));

            var files = Directory.GetFiles(path);

            sw.Stop();
            return (files, sw.ElapsedMilliseconds);
        }

        void WriteResults(String size, String results)
        {
            var path = System.IO.Path.GetFullPath(System.IO.Path.Join(this.root, "wpf-" + size + "-results.csv"));

            File.WriteAllText(path, results);
        }

        long[] CalculateAverage(long[][] results)
        {
            int testsLength = results[0].Length;
            long[] aggregation = new long[testsLength];

            foreach(var result in results) {
                for (int i = 0; i < result.Length; i++)
                {
                    aggregation[i] += result[i];                    
                }
            };

            for(int i = 0; i < aggregation.Length; i++)
            {
                aggregation[i] = aggregation[i] / results.Length;
            }

            return aggregation;
        }

        async Task<long[]> Benchmark(string size)
        {
            var (files, timeToList) = ReadFolderContent(size);
            var (filesLength, seqTime) = await ReadSequentially(files);
            //const [, parallelTime] = await readInParallel(files);

            return new long[] { timeToList, seqTime, 0 };
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
                    Status.Content = $"Running benchmark for size {size} ({i})";
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

            Status.Content = "Done";
            Start.IsEnabled = true;
        }
    }
}
