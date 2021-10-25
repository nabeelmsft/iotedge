using Microsoft.Azure.Devices;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace ClientApp
{
    class Program
    {
        private static ServiceClient serviceClient;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var connectionString = "[your-iothub-connection-string]";
            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            await InvokeMethodAsync();

            serviceClient.Dispose();
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
