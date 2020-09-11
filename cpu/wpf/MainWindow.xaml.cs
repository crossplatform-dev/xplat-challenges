using System;
using System.Collections.Generic;
using System.Linq;
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

namespace wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class PrimeProgressReport
        {
            //current progress
            public string Status { get; set; }
            public int Count { get; set; }
            public long Time { get; set; }
        }

        private double TotalTime = 0;

        async Task CalculatePrimes(int threshold, DateTime startTime, IProgress<PrimeProgressReport> progress)
        {
            static bool isPrime(int number)
            {
                if (number % 2 == 0 && number > 2)
                {
                    return false;
                }

                var start = 2;
                var limit = Math.Sqrt(number);
                while (start <= limit)
                {
                    if (number % start++ < 1)
                    {
                        return false;
                    }
                }
                return number > 1;
            };

            await Task.Run(() =>
            {
                int n = 0;
                int total = 0;
                var primes = new List<int>();

                DateTime previous = startTime;

                while (++n <= threshold)
                {
                    if (isPrime(n))
                    {
                        primes.Add(n);
                        total++;

                        var now = DateTime.Now;

                        if (now.Subtract(previous).TotalMilliseconds > 250)
                        {
                            previous = now;
                            progress.Report(new PrimeProgressReport { Status = "calculating", Count = total, Time = (long)now.Subtract(startTime).TotalMilliseconds });
                        }
                    }
                }

                progress.Report(new PrimeProgressReport { Status = "done", Count = total, Time = (long)DateTime.Now.Subtract(startTime).TotalMilliseconds });
            });
        }

        private void ReportProgress(PrimeProgressReport progress)
        {
            var prefix = "[Calculating]";

            if (progress.Status == "done")
            {
                prefix = "[Done]";
                Start.IsEnabled = true;

                TotalTime += progress.Time;
            }

            Status.Text = $"{prefix} Found {progress.Count} primer numbers in {progress.Time}ms";
        }


        public MainWindow()
        {
            InitializeComponent();
        }

        private async Task Benchmark()
        {
            int runs = 5;

            for (int i = 0; i < runs; i++)
            {
                var progressIndicator = new Progress<PrimeProgressReport>(ReportProgress);

                await CalculatePrimes(10000000, DateTime.Now, progressIndicator);
            }

            var averageTime = TotalTime / runs;

            Results.Text = $"Average time: {averageTime}ms";
            Start.IsEnabled = true;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Start.IsEnabled = false;
            // Start thread somehow

            Benchmark();
        }
    }
}
