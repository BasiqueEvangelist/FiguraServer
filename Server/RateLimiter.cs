using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FiguraServer.Server
{
    public class RateLimiter
    {
        private float points = 0;
        private float pointsPerSecond = 1;
        private float maxPoints;

        private DateTime lastTime;

        private object limiterLock = new object();

        public RateLimiter(float maxPoints, float pointsPerSecond = 1)
        {
            this.pointsPerSecond = pointsPerSecond;
            this.maxPoints = maxPoints;
            this.points = maxPoints;
            lastTime = DateTime.Now;
        }

        public bool TryTakePoints(float amount)
        {
            lock (limiterLock)
            {
                DateTime n = DateTime.Now;
                var diff = n - lastTime;
                lastTime = n;
                points = Math.Clamp(points + ((float)diff.TotalSeconds * pointsPerSecond), 0, maxPoints);

                if (points >= amount)
                {
                    points -= amount;
                    return true;
                }
                else
                {
                    points = 0;
                    return false;
                }
            }
        }
    }
}
