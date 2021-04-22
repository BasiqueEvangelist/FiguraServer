using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lib.AspNetCore.ServerSentEvents;
using FiguraServer.SSE.Services;
using Microsoft.AspNetCore.ResponseCompression;

namespace FiguraServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            #region SQL middleware
            if (!System.IO.File.Exists("sqlconnection.txt"))
            {
                throw new Exception("No 'sqlconnection.txt' file found, unable to initialize MySQL connections. Please create an sqlconnection.txt file containing the mysql connection string.");
            }

            string connection = System.IO.File.ReadAllText("sqlconnection.txt");

            services.AddTransient<AppDB>(_ => new AppDB(connection));
            #endregion

            #region SSE middleware
            // Register default ServerSentEventsService.
            services.AddServerSentEvents();

            // Registers custom ServerSentEventsService which will be used by second middleware, otherwise they would end up sharing connected users.
            services.AddServerSentEvents<INotificationsServerSentEventsService, NotificationsServerSentEventsService>(options =>
            {
                options.ReconnectInterval = 5000;
            });

            services.AddSingleton<IHostedService, HeartbeatService>();
            services.AddNotificationsService(Configuration);

            services.AddResponseCompression(options =>
            {
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "text/event-stream" });
            });
            services.AddControllersWithViews();
            #endregion

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FiguraServer", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FiguraServer v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseResponseCompression();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                // Set up first Server-Sent Events endpoint.
                endpoints.MapServerSentEvents("/see-heartbeat");

                // Set up second (separated) Server-Sent Events endpoint.
                endpoints.MapServerSentEvents<NotificationsServerSentEventsService>("/sse-notifications");

                endpoints.MapControllerRoute("default", "{controller=Notifications}/{action=sse-notifications-receiver}");

                //endpoints.MapControllers();

            });
        }
    }
}
