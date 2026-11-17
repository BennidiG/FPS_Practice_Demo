using UnityEngine;

namespace FpsPractice
{
    public sealed class SeededRandom
    {
        private uint state;

        public SeededRandom(uint seed)
        {
            state = seed == 0 ? 0x9E3779B9u : seed;
        }

        public uint NextUInt()
        {
            uint value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value;
            return value;
        }

        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public float Range(float minimum, float maximum)
        {
            return minimum + (maximum - minimum) * NextFloat();
        }

        public Vector2 InsideUnitCircle()
        {
            for (int i = 0; i < 16; i++)
            {
                Vector2 point = new Vector2(Range(-1f, 1f), Range(-1f, 1f));
                if (point.sqrMagnitude <= 1f)
                {
                    return point;
                }
            }

            float angle = Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
