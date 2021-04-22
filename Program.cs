using FiguraServer.FiguraServer.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer
{
    public class Program
    {

        public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        public static async Task MainAsync(string[] args)
        {
            try
            {
                Task webAppTask = new Task(() =>
                {
                    Host.CreateDefaultBuilder(args)
                        .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.UseStartup<Startup>();
                        }).Build().Run();
                });
                webAppTask.Start();

                Task minecraftServerTask = FiguraAuthServer.Start();
                minecraftServerTask.Start();

                await webAppTask;
                await minecraftServerTask;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
