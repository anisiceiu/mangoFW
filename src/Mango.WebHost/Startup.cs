using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using System.IO;
using Mango.WebHost.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;

using Mango.Framework;
using Mango.Framework.Data;
using Microsoft.AspNetCore.Diagnostics;
using NLog;
namespace Mango.WebHost
{
    public class Startup
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IWebHostEnvironment _webHostEnvironment;
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        public IConfiguration Configuration { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            GlobalConfiguration.ContentRootPath = _webHostEnvironment.ContentRootPath;
            services.AddModules(GlobalConfiguration.ContentRootPath);
            services.AddCustomizedMvc();

            //services.AddCustomizedSwagger(GlobalConfiguration.ContentRootPath);
            //?־û?ע??
            services.AddDbContext<MangoDbContext>(opts =>
            {
                opts.UseSqlServer(Configuration.GetConnectionString("SqlServer"));
            });
            services.AddScoped(typeof(IUnitOfWork<MangoDbContext>), typeof(UnitOfWork<MangoDbContext>));

            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            //??Ȩ????
            services.AddCustomizedAuthorization(_webHostEnvironment);
            //??????????ע????Ҫ?ŵ?????
            services.AddCustomizedServices(Configuration);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles();
            
            //
            app.UseCustomizedMvc(env);
            app.UseCustomizedModuleConfigure(env);
            app.UseCustomizedMiddleware();
        }
    }
}
