using Microsoft.Azure.Devices;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Context.Propagation;
using System.Collections.Generic;
using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using System.Net.Http.Headers;

using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using OpenTelemetry.Context;

namespace ClientApp
{
    class Program
    {
        private static ServiceClient serviceClient;
        private static ActivitySource source = new ActivitySource("Sample.DistributedTracing", "1.0.0");
        private static readonly TextMapPropagator propagator = Propagators.DefaultTextMapPropagator;
        private static readonly Action<HttpRequestMessage, string, string> HttpRequestMessageHeaderValueSetter = (request, name, value) => request.Headers.Add(name, value);
        //private readonly HttpClientInstrumentationOptions options;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            
            var connectionString = "HostName=new-shalimar.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=vy6Y3RZqHoAOBUfPxpNIDpb92cLxAhrjWgrKLyRj2nk=";
            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);


            var resourceAttributes = new Dictionary<string, object> { { "service.name", "my-service" }, { "service.namespace", "my-namespace" }, { "service.instance.id", "my-instance" } };
            var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);



            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)

                .AddSource("Sample.DistributedTracing")
                .AddConsoleExporter()
                .AddAzureMonitorTraceExporter(o =>
                {
                    o.ConnectionString = $"InstrumentationKey=7df1e5db-aae5-4fa0-a428-49b539b7c9af;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/";
                })
                //.AddConsoleExporter()
                .Build();
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            using (Activity activity = source.StartActivity("Sample.DistributedTracing"))
            {
                
                ActivityContext activityContextToInject = default;
                if(activity != null)
                {
                    activityContextToInject = activity.Context;
                }
                else if (Activity.Current != null)
                {
                    activityContextToInject = Activity.Current.Context;
                }

                var traceParentKeyValue = new KeyValuePair<string, string>("traceparent", activity.Id);
                var traceStateKeyValue = new KeyValuePair<string, string>("tracestate", "congo=t61rcWkgMzE");
                var headerDictionary = new Dictionary<string, string>();
                headerDictionary.Add("traceparent", activity.Id);
                headerDictionary.Add("tracestate", "congo=t61rcWkgMzE");

                //propagator.Inject(activityContextToInject, headerDictionary);
                //propagator.Inject(new PropagationContext(activityContextToInject, Baggage.Current),headerDictionary);
                //await DoSomeWork("banana", 8);
                Console.WriteLine("Example work done");
                // All the functions below simulate doing some arbitrary work
                await InvokeMethodUsingHTTPAsync(activity);

                serviceClient.Dispose();
            }
        }


        static async Task DoSomeWork(string foo, int bar)
        {
            
            await StepOne();
            await StepTwo();
            
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
        private static async Task InvokeMethodAsync(Activity activity)
        {
            var methodInvocation = new CloudToDeviceMethod("SetTelemetryInterval")
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };
            methodInvocation.SetPayloadJson("12");
            ActivityContext activityContextToInject = default;
            if (activity != null)
            {
                activityContextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                activityContextToInject = Activity.Current.Context;
            }
            propagator.Inject<CloudToDeviceMethod>(new PropagationContext(activity.Context, Baggage.Current), methodInvocation, null);// (Action<CloudToDeviceMethod, string, string>)HttpRequestMessageHeaderValueSetter);
            // Invoke the direct method asynchronously and get the response from the simulated device.
            var response = await serviceClient.InvokeDeviceMethodAsync("new-nabeel-percept-device", "edgesolution", methodInvocation);

            Console.WriteLine($"\nResponse status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");
        }

        private static async Task InvokeMethodUsingHTTPAsync(Activity activity)
        {
            // Command to generate token
            // az iot hub generate-sas-token -n [iot-hub-name] --du 1003600
            Console.WriteLine($"\nReceived Activity:{ activity }");
            Console.WriteLine($"\nReceived Activity.TraceId:{ activity.TraceId }");
            Console.WriteLine($"\nReceived Activity.TraceStateString:{ activity.TraceStateString }");
            Console.WriteLine($"\nReceived Activity.SpanId:{ activity.SpanId }");
            Console.WriteLine($"\nReceived Activity.Baggage:{ activity.Baggage }");
            Console.WriteLine($"\nReceived Activity.Context:{ activity.Context }");
            Console.WriteLine($"\nReceived Activity.Context.SpanId:{ activity.Context.SpanId }");
            Console.WriteLine($"\nReceived Activity.Context.TraceId:{ activity.Context.TraceId }");

            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://new-shalimar.azure-devices.net/twins/new-nabeel-percept-device/modules/edgesolution/methods?api-version=2018-06-30"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "SharedAccessSignature sr=new-shalimar.azure-devices.net&sig=ubHSFWY8dG9oDD0t%2FUPpQ4b8Rz3wcjrypOWThIeWi2A%3D&se=1637570626&skn=iothubowner");

                    request.Content = new StringContent("{\n    \"methodName\": \"reboot\",\n    \"responseTimeoutInSeconds\": 200,\n    \"payload\": {\n        \"input1\": \"someInput\",\n        \"input2\": \"anotherInput\"\n    }\n}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), request, HttpRequestMessageHeaderValueSetter);
                    var httpResponseMessage = await httpClient.SendAsync(request);
                    Console.WriteLine($"\nResponse status: {httpResponseMessage.StatusCode}, payload:\n\t{httpResponseMessage.Content}");
                }
            }

            
        }
    }
}
