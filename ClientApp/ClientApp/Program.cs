using Microsoft.Azure.Devices;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;

namespace ClientApp
{
    class Program
    {
        private static ServiceClient serviceClient;
        private static ActivitySource source = new ActivitySource("Sample.DistributedTracing", "1.0.0");
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var connectionString = "[your-iothub-connection-string]";
            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            await InvokeMethodAsync();


            var resourceAttributes = new Dictionary<string, object> { { "service.name", "my-service" }, { "service.namespace", "my-namespace" }, { "service.instance.id", "my-instance" } };
            var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource("Demo.DemoServer")
                .AddSource("Demo.DemoClient")
                .AddSource("Sample.DistributedTracing")
                .AddConsoleExporter()
                .AddAzureMonitorTraceExporter(o =>
                {
                    o.ConnectionString = $"InstrumentationKey=7df1e5db-aae5-4fa0-a428-49b539b7c9af;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/";
                })
                //.AddConsoleExporter()
                .Build();

            await DoSomeWork("banana", 8);
            Console.WriteLine("Example work done");
            // All the functions below simulate doing some arbitrary work


            serviceClient.Dispose();
        }
        static async Task DoSomeWork(string foo, int bar)
        {
            using (Activity activity = source.StartActivity("SomeWork"))
            {
                await StepOne();
                await StepTwo();
            }
        }

        static async Task StepOne()
        {
            await Task.Delay(500);
        }

        static async Task StepTwo()
        {
            await Task.Delay(1000);
        }

        // Invoke the direct method on the device, passing the payload
        private static async Task InvokeMethodAsync()
        {
            var methodInvocation = new CloudToDeviceMethod("SetTelemetryInterval")
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };
            methodInvocation.SetPayloadJson("10");

            // Invoke the direct method asynchronously and get the response from the simulated device.
            var response = await serviceClient.InvokeDeviceMethodAsync("new-nabeel-percept-device", "edgesolution", methodInvocation);

            Console.WriteLine($"\nResponse status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");
        }
    }
}
