using System.Timers;


namespace NebularApi.DataCollectors
{
    /// <summary>
    /// Extract data about top exchanges in past 24 hours based on volume.
    /// </summary>
    /// <remarks>
    /// Exchanges are filtered through blacklist of scam tokens.
    /// </remarks>
    internal class TopExchangesCollector
    {
        private string _horizonUrl;
        private Timer _timer;

        internal TopExchangesCollector(string horizonApiUrl, int intervalMinutes)
        {
            _horizonUrl = horizonApiUrl.TrimEnd('/') + "/trades?order=desc&limit=200";
            _timer = new Timer(intervalMinutes * 60 * 1000);
        }


        internal void Start()
        {
            _timer.AutoReset = true;
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            System.Console.WriteLine("DEBUG: Top Exchanges collector timer elapsed");

            var webClient = new System.Net.WebClient();
            string json = webClient.DownloadString(_horizonUrl);

            int debug = new System.Text.RegularExpressions.Regex("paging_token").Matches(json).Count;
            System.Console.ForegroundColor = System.ConsoleColor.Magenta;
            System.Console.WriteLine($"Found {debug} records");
        }
    }
}
