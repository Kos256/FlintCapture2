using System;
using System.Collections.Generic;
using System.Text;

namespace FlintCapture2.Scripts
{
    public class ExtraUtils
    {
        public static string PickWeightedMessage(Dictionary<string, float> messages)
        {
            // Sum up all probabilities
            float total = messages.Values.Sum();

            // Pick a random number between 0 and total
            float roll = (float)(Random.Shared.NextDouble() * total);

            float cumulative = 0f;
            foreach (var kvp in messages)
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                {
                    return kvp.Key;
                }
            }

            // Fallback (should never happen if probabilities > 0)
            return messages.Keys.Last();
        }
    }
}
