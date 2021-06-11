using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server
{
    public class RateLimiterGroup
    {
        //Byte limiter, limits the maximum byte throughput of the entire connection.
        //200kb capacity
        //20kb/s recovery speed.
        public RateLimiter byteRateLimiter = new RateLimiter(1024 * 200, 1024 * 20);

        //Message rate limiter, limits the maximum count of messages through the entire connection.
        //2048 capacity
        //256/s recovery speed
        public RateLimiter messageRateLimiter = new RateLimiter(2048, 256);

        //Avatar upload rate limiter
        //4 capacity
        //1/s recovery speed
        public RateLimiter avatarUploadRateLimiter = new RateLimiter(4);

        //Avatar request rate limiter
        //2048 capacity
        //1/s recovery speed
        public RateLimiter avatarRequestRateLimiter = new RateLimiter(2048);

        //Ping byte rate limiter
        //2kb capacity
        //1kb/s recovery speed
        public RateLimiter pingByteRateLimiter = new RateLimiter(2048, 1024);

        //Ping rate limiter, limits the maximum amount of ping messages that can be sent through the connection.
        //21 capacity
        //21/s recovery speed
        public RateLimiter pingRateLimiter = new RateLimiter(21, 21);

        public bool IsFull()
        {
            return byteRateLimiter.IsFull() &&
                messageRateLimiter.IsFull() &&
                avatarUploadRateLimiter.IsFull() &&
                avatarRequestRateLimiter.IsFull() &&
                pingByteRateLimiter.IsFull() &&
                pingRateLimiter.IsFull();
        }
    }
}
