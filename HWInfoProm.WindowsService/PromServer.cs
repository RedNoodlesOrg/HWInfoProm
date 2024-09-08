using Prometheus;

namespace HWInfoProm.WindowsService
{
    static class PromServer
    {
        private static readonly Dictionary<string, string> unitMap = new()
        {
            { "Â°C", "celsius" },
            { "%", "ratio" },
            { "Yes/No", "flag" },
            { "W", "watts" },
            { "V", "volts" },
            { "dB", "decibel" },
            { "RPM", "rpm" },
            { "GB", "gigabytes" }
        };
        private static Dictionary<(string ReadingType, string Unit), Gauge>? gauges;
        private static MetricServer? metricServer;
        public static void Start(int port)
        {
            HWHash.Launch();
            Metrics.SuppressDefaultMetrics();
            gauges = GetGauges(HWHash.GetOrderedList());
            metricServer = new MetricServer(port: port);
            metricServer.Start();
        }

        public static void Update()
        {
            if (gauges == null || metricServer == null) return;

            foreach (var record in HWHash.GetOrderedList())
            {
                gauges[(record.ReadingType, GetUnit(record.Unit))].WithLabels(record.ParentNameCustom, record.NameCustom).Set(record.ValueNow);
            }
        }

        public static void Stop()
        {
            metricServer?.Stop();
        }
        private static Dictionary<(string ReadingType, string Unit), Gauge> GetGauges(List<HWHash.HWINFO_HASH> sensors)
        {
            List<(string ReadingType, string Unit)> readingTypeUnitTuples = sensors
                .Select(sensor => (sensor.ReadingType, GetUnit(sensor.Unit)))
                .Distinct()
                .ToList();
            Dictionary<(string ReadingType, string Unit), Gauge> gauges = [];
            foreach (var sensor in readingTypeUnitTuples)
            {
                Gauge g = Metrics.CreateGauge(
                    $"hwinfo_{sensor.ReadingType.ToLower()}_{sensor.Unit}",
                    $"{sensor.ReadingType} reading in {sensor.Unit} of a sensor.",
                    new GaugeConfiguration
                    {
                        LabelNames = ["parentName", "sensorName"]
                    });
                gauges.Add(sensor, g);
            }
            return gauges;
        }

        private static string GetUnit(string input)
        {
            if (!unitMap.TryGetValue(input, out string? unit))
            {
                unit = "unknown";
            }
            return unit;
        }
    }
}
