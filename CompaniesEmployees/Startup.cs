using AspNetCoreRateLimit;
using CompaniesEmployees.ActionFilters;
using CompaniesEmployees.Extensions;
using CompaniesEmployees.Utility;
using Contracts;
using Entities.DataTransferObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using Repository;
using Repository.DataShaping;
using System.IO;

namespace CompaniesEmployees
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            LogManager.LoadConfiguration(string.Concat(Directory.GetCurrentDirectory(), "/nlog.config"));
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureCors();
            services.ConfigureLoggerService();
            services.ConfigureSqlContext(Configuration);
            services.ConfigureRepositoryManager();
            services.AddAutoMapper(typeof(Startup));
            services.Configure<ApiBehaviorOptions>(options => {
                options.SuppressModelStateInvalidFilter = true;
            });
            services.ConfigureSwagger();
            services.AddScoped<ValidationFilterAttribute>();
            services.AddScoped<ValidateCompanyExistsAttribute>();
            services.AddScoped<ValidateEmployeeForCompanyExistsAttribute>();
            services.AddScoped<IDataShaper<EmployeeDto>, DataShaper<EmployeeDto>>();
            services.AddScoped<ValidateMediaTypeAttribute>();
            services.AddScoped<EmployeeLinks>();
            services.AddScoped<IAuthenticationManager, AuthenticationManager>();

            services.AddMemoryCache();
            services.ConfigureRateLimitingOptions();
            services.AddHttpContextAccessor();

            services.Configure<ApiBehaviorOptions>(options => {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddHttpContextAccessor();
            //services.ConfigureVersioning();
            services.ConfigureResponseCaching();
            services.ConfigureHttpCacheHeaders();

            services.ConfigureIdentity();
            services.ConfigureJWT(Configuration);

            services.AddAuthentication();
            services.ConfigureIdentity();

            services.AddControllers(config => {
                config.RespectBrowserAcceptHeader = true;
                config.ReturnHttpNotAcceptable = true;
                config.CacheProfiles.Add("120SecondsDuration", new CacheProfile { Duration = 120 });
            }).AddNewtonsoftJson()
              .AddXmlDataContractSerializerFormatters()
              .AddCustomCSVFormater();

            services.AddCustomMediaTypes();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerManager logger)
        {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseHsts();
            }

            app.ConfigureExceptionHandler(logger);
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCors("CorsPolicy");

            app.UseForwardedHeaders(new ForwardedHeadersOptions {
                ForwardedHeaders = ForwardedHeaders.All
            });

            app.UseResponseCaching();
            app.UseHttpCacheHeaders();

            app.UseIpRateLimiting();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI(s => {
                s.SwaggerEndpoint("/swagger/v1/swagger.json", "Company Employees");
            });

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}
