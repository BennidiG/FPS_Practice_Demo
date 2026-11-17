using System.Collections.Generic;
using System.Text;

namespace FpsPractice
{
    public sealed class TrainingMetrics
    {
        private readonly List<float> reactionTimes = new List<float>();
        private readonly List<float> killTimes = new List<float>();
        private readonly int[] zoneHits = new int[6];

        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public int Kills { get; private set; }
        public int Score { get; private set; }
        public int CurrentStreak { get; private set; }
        public int BestStreak { get; private set; }
        public float Accuracy { get { return Shots == 0 ? 0f : (float)Hits / Shots; } }
        public float LastReactionMs { get; private set; }
        public float LastTtkMs { get; private set; }
        public float AverageReactionMs { get { return Average(reactionTimes); } }
        public float AverageTtkMs { get { return Average(killTimes); } }
        public float BestReactionMs { get { return reactionTimes.Count == 0 ? 0f : Min(reactionTimes); } }
        public float BestTtkMs { get { return killTimes.Count == 0 ? 0f : Min(killTimes); } }
        public float MedianReactionMs { get { return TrainingAnalytics.Percentile(reactionTimes, 0.5f); } }
        public float MedianTtkMs { get { return TrainingAnalytics.Percentile(killTimes, 0.5f); } }
        public float P95ReactionMs { get { return TrainingAnalytics.Percentile(reactionTimes, 0.95f); } }
        public float P95TtkMs { get { return TrainingAnalytics.Percentile(killTimes, 0.95f); } }
        public float MousePathEfficiency { get; private set; }

        public void RecordMiss()
        {
            Shots++;
            CurrentStreak = 0;
        }

        public void RecordHit(TargetHitResult result, float secondsSinceSpawn)
        {
            Shots++;
            Hits++;
            Score += result.Score;
            CurrentStreak++;
            BestStreak = BestStreak < CurrentStreak ? CurrentStreak : BestStreak;
            zoneHits[(int)result.Zone]++;

            if (result.FirstHit)
            {
                LastReactionMs = secondsSinceSpawn * 1000f;
                reactionTimes.Add(LastReactionMs);
            }

            if (result.Killed)
            {
                Kills++;
                LastTtkMs = secondsSinceSpawn * 1000f;
                killTimes.Add(LastTtkMs);
            }
        }

        public int GetZoneHits(TargetHitZone zone)
        {
            return zoneHits[(int)zone];
        }

        public float[] GetReactionTimes()
        {
            return reactionTimes.ToArray();
        }

        public float[] GetKillTimes()
        {
            return killTimes.ToArray();
        }

        public void SetMousePathEfficiency(float efficiency)
        {
            if (float.IsNaN(efficiency) || efficiency <= 0f)
            {
                MousePathEfficiency = 0f;
            }
            else if (efficiency >= 1f)
            {
                MousePathEfficiency = 1f;
            }
            else
            {
                MousePathEfficiency = efficiency;
            }
        }

        public TrainingRecord BuildRecord(string scenario, string weapon, uint seed)
        {
            return new TrainingRecord
            {
                scenario = scenario,
                weapon = weapon,
                seed = seed,
                shots = Shots,
                hits = Hits,
                kills = Kills,
                score = Score,
                bestStreak = BestStreak,
                accuracy = Accuracy,
                reactionMedian = MedianReactionMs,
                reactionP95 = P95ReactionMs,
                ttkMedian = MedianTtkMs,
                ttkP95 = P95TtkMs,
                mousePathEfficiency = MousePathEfficiency,
                trackingTimeRatio = 0f
            };
        }

        public string BuildSummaryText()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("TRAINING SUMMARY");
            builder.AppendLine("Shots: " + Shots + "   Hits: " + Hits + "   Accuracy: " + (Accuracy * 100f).ToString("0.0") + "%");
            builder.AppendLine("Kills: " + Kills + "   Score: " + Score + "   Best streak: " + BestStreak);
            builder.AppendLine("Reaction avg/best: " + AverageReactionMs.ToString("0") + " / " + BestReactionMs.ToString("0") + " ms");
            builder.AppendLine("TTK avg/best: " + AverageTtkMs.ToString("0") + " / " + BestTtkMs.ToString("0") + " ms");
            builder.AppendLine("Bullseye: " + zoneHits[0] + "   Critical: " + zoneHits[1] + "   Inner: " + zoneHits[2]);
            builder.AppendLine("Outer: " + zoneHits[3] + "   Rim: " + zoneHits[4] + "   Frame: " + zoneHits[5]);
            builder.AppendLine("F6 close   F4 restart   F3 next scenario");
            return builder.ToString();
        }

        public void Reset()
        {
            Shots = 0;
            Hits = 0;
            Kills = 0;
            Score = 0;
            CurrentStreak = 0;
            BestStreak = 0;
            LastReactionMs = 0f;
            LastTtkMs = 0f;
            MousePathEfficiency = 0f;
            reactionTimes.Clear();
            killTimes.Clear();
            for (int i = 0; i < zoneHits.Length; i++)
            {
                zoneHits[i] = 0;
            }
        }

        private static float Average(List<float> values)
        {
            if (values.Count == 0) return 0f;
            float total = 0f;
            for (int i = 0; i < values.Count; i++) total += values[i];
            return total / values.Count;
        }

        private static float Min(List<float> values)
        {
            float result = float.MaxValue;
            for (int i = 0; i < values.Count; i++) result = result > values[i] ? values[i] : result;
            return result;
        }
    }
}
