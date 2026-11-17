using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace FpsPractice
{
    [Serializable]
    public sealed class TrainingRecord
    {
        public string scenario;
        public string weapon;
        public string utcTimestamp;
        public uint seed;
        public int shots;
        public int hits;
        public int kills;
        public int score;
        public int bestStreak;
        public float accuracy;
        public float reactionMedian;
        public float reactionP95;
        public float ttkMedian;
        public float ttkP95;
        public float mousePathEfficiency;
        public float trackingTimeRatio;
    }

    public static class TrainingHistoryStore
    {
        public const int Limit = 20;

        private static readonly List<TrainingRecord> records = new List<TrainingRecord>();
        private static readonly ReadOnlyCollection<TrainingRecord> recordsView = records.AsReadOnly();

        public static IReadOnlyList<TrainingRecord> Records { get { return recordsView; } }

        public static void Add(TrainingRecord record)
        {
            if (record == null) return;
            records.Add(record);
            while (records.Count > Limit)
            {
                records.RemoveAt(0);
            }
        }

        public static void Clear()
        {
            records.Clear();
        }

        public static TrainingRecord GetPersonalBest(string scenario)
        {
            TrainingRecord best = null;
            for (int i = 0; i < records.Count; i++)
            {
                TrainingRecord candidate = records[i];
                if (!string.Equals(candidate.scenario, scenario, StringComparison.Ordinal)) continue;

                if (best == null ||
                    candidate.score > best.score ||
                    (candidate.score == best.score && candidate.accuracy > best.accuracy) ||
                    (candidate.score == best.score && candidate.accuracy == best.accuracy &&
                     candidate.reactionMedian < best.reactionMedian))
                {
                    best = candidate;
                }
            }

            return best;
        }

        public static string BuildJson()
        {
            JsonRecordsWrapper wrapper = new JsonRecordsWrapper();
            wrapper.records = records.ToArray();
            return UnityEngine.JsonUtility.ToJson(wrapper, true);
        }

        public static void LoadJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            JsonRecordsWrapper wrapper = UnityEngine.JsonUtility.FromJson<JsonRecordsWrapper>(json);
            if (wrapper == null || wrapper.records == null) return;
            records.Clear();
            for (int i = 0; i < wrapper.records.Length; i++)
            {
                Add(wrapper.records[i]);
            }
        }

        public static string BuildCsv()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("scenario,weapon,utcTimestamp,seed,shots,hits,kills,score,bestStreak,accuracy,reactionMedian,reactionP95,ttkMedian,ttkP95,mousePathEfficiency,trackingTimeRatio");
            CultureInfo culture = CultureInfo.InvariantCulture;

            for (int i = 0; i < records.Count; i++)
            {
                TrainingRecord record = records[i];
                string[] fields = new string[]
                {
                    EscapeCsv(record.scenario),
                    EscapeCsv(record.weapon),
                    EscapeCsv(record.utcTimestamp),
                    record.seed.ToString(culture),
                    record.shots.ToString(culture),
                    record.hits.ToString(culture),
                    record.kills.ToString(culture),
                    record.score.ToString(culture),
                    record.bestStreak.ToString(culture),
                    record.accuracy.ToString("R", culture),
                    record.reactionMedian.ToString("R", culture),
                    record.reactionP95.ToString("R", culture),
                    record.ttkMedian.ToString("R", culture),
                    record.ttkP95.ToString("R", culture),
                    record.mousePathEfficiency.ToString("R", culture),
                    record.trackingTimeRatio.ToString("R", culture)
                };
                builder.AppendLine(string.Join(",", fields));
            }

            return builder.ToString();
        }

        public static string BuildHistoryText(int max = 5)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("TRAINING HISTORY");
            if (records.Count == 0)
            {
                builder.AppendLine("No records yet.");
                return builder.ToString();
            }

            int count = Math.Min(records.Count, Math.Max(0, max));
            CultureInfo culture = CultureInfo.InvariantCulture;
            for (int i = 0; i < count; i++)
            {
                TrainingRecord record = records[records.Count - 1 - i];
                builder.Append(record.utcTimestamp);
                builder.Append(" | ");
                builder.Append(record.scenario);
                builder.Append(" | ");
                builder.Append(record.weapon);
                builder.Append(" | Score: ");
                builder.Append(record.score.ToString(culture));
                builder.Append(" | Accuracy: ");
                builder.Append((record.accuracy * 100f).ToString("0.0", culture));
                builder.Append("% | Reaction med/P95: ");
                builder.Append(record.reactionMedian.ToString("0", culture));
                builder.Append("/");
                builder.Append(record.reactionP95.ToString("0", culture));
                builder.Append(" ms | TTK med/P95: ");
                builder.Append(record.ttkMedian.ToString("0", culture));
                builder.Append("/");
                builder.Append(record.ttkP95.ToString("0", culture));
                builder.Append(" ms | Mouse path: ");
                builder.Append((record.mousePathEfficiency * 100f).ToString("0.0", culture));
                builder.Append("% | Tracking: ");
                builder.Append((record.trackingTimeRatio * 100f).ToString("0.0", culture));
                builder.AppendLine("%");
            }

            return builder.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.IndexOf(',') < 0 && value.IndexOf('"') < 0 &&
                value.IndexOf('\r') < 0 && value.IndexOf('\n') < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        [Serializable]
        private sealed class JsonRecordsWrapper
        {
            public TrainingRecord[] records;
        }
    }
}
