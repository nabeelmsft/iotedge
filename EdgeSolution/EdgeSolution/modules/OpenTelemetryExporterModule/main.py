# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for
# full license information.

import time
import os
from opentelemetry import trace
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor

from azure.monitor.opentelemetry.exporter import AzureMonitorTraceExporter

import sys
import asyncio
from six.moves import input
import threading
from azure.iot.device.aio import IoTHubModuleClient
from azure.iot.device import Message, MethodResponse


async def main():
    try:
        if not sys.version >= "3.5.3":
            raise Exception( "The sample requires python 3.5.3+. Current version of Python: %s" % sys.version )
        print ( "Version 1.0.0.11 === IoT Hub Client for Python. M Nabeel Khan." )
        print (os.environ["APPLICATIONINSIGHTS_CONNECTION_STRING"])
        exporter = AzureMonitorTraceExporter.from_connection_string(
            os.environ["APPLICATIONINSIGHTS_CONNECTION_STRING"]
        )

        print("Created exporter")

        trace.set_tracer_provider(TracerProvider())
        tracer = trace.get_tracer(__name__)
        span_processor = BatchSpanProcessor(exporter)
        trace.get_tracer_provider().add_span_processor(span_processor)

        with tracer.start_as_current_span("hello"):
            print("Hello, World!")

        print("Done exporter")

        # The client object is used to interact with your Azure IoT hub.
        module_client = IoTHubModuleClient.create_from_edge_environment()

        # connect the client.
        await module_client.connect()

        # define behavior for receiving an input message on input1
        async def input1_listener(method_request):
            print(method_request)
            print("the data in the message received on input1 was ")
            print(method_request.name)
            print(method_request.payload)
            response_payload = {"Response": "Executed direct method {}".format(method_request.name)}
            response_status = 200
            method_response = MethodResponse.create_from_method_request(method_request, response_status, response_payload)
            await module_client.send_method_response(method_response)

            # while True:
            #     # input_message = await module_client.receive_message_on_input("input1")  # blocking call
            #     print("the data in the message received on input1 was ")
            #     print(input_message.data)
            #     print("custom properties are")
            #     print(input_message.custom_properties)
            #     print("forwarding mesage to output1")
            #     await module_client.send_message_to_output(input_message, "output1")

        # define behavior for halting the application
        def stdin_listener():
            while True:
                try:
                    # with tracer.start_as_current_span("hello1"):
                    #     print("Hello, World1!")
                    #     print("Done exporter1")                                            
                    selection = input("Press Q to quit...Added requirements\n")
                    if selection == "Q" or selection == "q":
                        print("Quitting...")
                        break
                except:
                    time.sleep(10)

        # Schedule task for C2D Listener
        #listeners = asyncio.gather(input1_listener(module_client))
        module_client.on_method_request_received = input1_listener
        print ( "The sample is now waiting for messages. ")

        # Run the stdin listener in the event loop
        loop = asyncio.get_event_loop()
        user_finished = loop.run_in_executor(None, stdin_listener)

        # Wait for user to indicate they are done listening for messages
        await user_finished

        # Cancel listening
        listeners.cancel()

        # Finally, disconnect
        await module_client.disconnect()

    except Exception as e:
        print ( "Unexpected error %s " % e )
        raise

if __name__ == "__main__":
    loop = asyncio.get_event_loop()
    loop.run_until_complete(main())
    loop.close()

    # If using Python 3.7 or above, you can use following code instead:
    # asyncio.run(main())