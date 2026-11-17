namespace FpsPractice
{
    public sealed class PracticeStats
    {
        public int Shots { get; private set; }
        public int Hits { get; private set; }

        public float Accuracy
        {
            get { return Shots == 0 ? 0f : (float)Hits / Shots; }
        }

        public void RegisterShot(bool hit)
        {
            Shots++;
            if (hit)
            {
                Hits++;
            }
        }

        public void Reset()
        {
            Shots = 0;
            Hits = 0;
        }
    }
}
