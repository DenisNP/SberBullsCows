using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SberBullsCows.Abstract;
using SberBullsCows.Models;
using SberBullsCows.Services;

namespace SberBullsCows
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddCors();
            services.AddMemoryCache();

            services.AddSingleton<IStateStorage<UserState>, SberDbStateStorage>();
            services.AddSingleton<IStateStorage<SessionState>, MemcacheStateStorage>();
            services.AddSingleton<SaluteService>();
            services.AddSingleton<ContentService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ContentService contentService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapControllers();
            });
            
            contentService.Load();
        }
    }
}