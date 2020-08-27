using System;
using Discord;
using System.Collections.Generic;

namespace donniebot.services
{
    public class RandomService
    {
        private readonly Random _random;
        private List<int> ids = new List<int>();

        public RandomService(Random random) => _random = random;

        public int RandomNumber(int min, int max) => _random.Next(min, max);
        public float RandomFloat(float max) => (float)_random.NextDouble() * max;
        public float RandomFloat(float max, float min) => (float)_random.NextDouble() * (max - min) + min;

        public Color RandomColor()
        {
            Random r = new Random();
            uint clr = Convert.ToUInt32(r.Next(0, 0xFFFFFF));
            return new Color(clr);
        }

        public int GenerateId()
        {
            int generated;
            do
            {
                generated = _random.Next();
            }
            while (ids.Contains(generated));
            ids.Add(generated);
            return generated;
        }
    }
}