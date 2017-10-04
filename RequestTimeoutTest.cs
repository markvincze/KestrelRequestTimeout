using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KestrelRequestTimeout
{
    public class RequestTimeoutTest
    {
        [Fact]
        public async Task SendResponseWithoutReading_ResponseShouldBeSentProperly()
        {
            using (var webHost = Repro.StartServer())
            {
                // Dummy content string used as the request, containing 7000 characters.
                var requestContent = string.Join("", Enumerable.Range(0, 1000).Select(x => "abcdefg"));
                
                // If we would use this smaller request content, then the issue doesn't happen.
                // var requestContent = "abcdefg";

                using (var httpClient = new HttpClient())
                {
                    for (var i = 0; i < 10; i++)
                    {
                        var postContent = new StringContent(requestContent, Encoding.UTF8, "text/plain");
                        Console.WriteLine($"Sending REQUEST {i}");
                        using (var response = await httpClient.PostAsync(Repro.Address, postContent))
                        {
                            Console.WriteLine($"Received RESPONSE {i}");
                            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                            var responseContent = await response.Content.ReadAsStringAsync();
                            Assert.Equal(Repro.TestResponse, responseContent);
                        }
                    }
                }
            }
        }
    }

    public static class Repro 
    {
        public static string Address { get; } = "http://localhost:5005/";
        public static string TestResponse = "TestResponse";

        public static IWebHost StartServer()
        {
            var startup = new ReproStartup();

            var hostBuilder = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls(Address)
                    .ConfigureServices(startup.ConfigureServices)
                    .Configure(startup.Configure);

            var webHost = hostBuilder.Build();
            webHost.Start();
            
            return webHost;
        }
    }

    public class ReproStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async httpContext =>
            {
                httpContext.Response.StatusCode = 200;
                
                // If we add this line here to completely read the request stream before responding, the problem disappears.
                //await ReadToEnd(httpContext.Request.Body);

                await httpContext.Response.WriteAsync(Repro.TestResponse);
            });
        }

        private async Task ReadToEnd(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                await sr.ReadToEndAsync();
            }
        }
    }
}
