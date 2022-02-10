# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for
# full license information.

import time
import os
from azure.core.pipeline import Pipeline
from azure.core.pipeline.policies import (
    BearerTokenCredentialPolicy,
    ContentDecodePolicy,
    DistributedTracingPolicy,
    HeadersPolicy,
    HttpLoggingPolicy,
    NetworkTraceLoggingPolicy,
    UserAgentPolicy,
)

from opentelemetry import baggage, trace
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
#from opentelemetry.ext import tags
from opentelemetry.propagate import set_global_textmap
#from opentelemetry.propagation import Format
from azure.monitor.opentelemetry.exporter import AzureMonitorTraceExporter
from opentelemetry.trace.propagation import _SPAN_KEY
from opentelemetry.context import Context, get_current
from opentelemetry.propagators.textmap import DefaultGetter

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
        print ( "Version 1.0.0.49 === IoT Hub Client for Python. M Nabeel Khan." )
        print (os.environ["APPLICATIONINSIGHTS_CONNECTION_STRING"])
        print("printing span")
        print(_SPAN_KEY)
        #clientWithHttpPipeline = IoTHubModuleClient(http_pipeline = )
        # The client object is used to interact with your Azure IoT hub.
        module_client = IoTHubModuleClient.create_from_edge_environment(websockets = True)

        # connect the client.
        await module_client.connect()

        # define behavior for receiving an input message on input1
        async def input1_listener(method_request):
            exporter = AzureMonitorTraceExporter.from_connection_string(
                os.environ["APPLICATIONINSIGHTS_CONNECTION_STRING"]
            )
            print("Created exporter within the method")
            print(trace)            

            trace.set_tracer_provider(TracerProvider())
            print("__name__")
            print(__name__)
            tracer = trace.get_tracer(__name__)
            print("Getting trace detail")
            print(tracer)
            print("Printing what is in tracer")
            print(tracer.__dict__)
            print("getting more what is in tracer by going object by object")
            print("Printing tracer.resource")
            print(tracer.resource)
            print("Printing what is in tracer.resource")
            print(tracer.resource.__dict__)
            print("Printing tracer.sampler")
            print(tracer.sampler)
            print("Printing what is in tracer.sampler")
            print(tracer.sampler.__dict__)            
            print("Printing tracer.span_processor")
            print(tracer.span_processor)
            print("Printing what is in tracer.span_processor")
            print(tracer.span_processor.__dict__) 
            print("Printing tracer.span_processor._span_processors")
            print(tracer.span_processor._span_processors)

            print("Printing tracer.id_generator")
            print(tracer.id_generator)
            print("Printing what is in tracer.id_generator")
            print(tracer.id_generator.__dict__)   

            print("Printing tracer.instrumentation_info")
            print(tracer.instrumentation_info)


            print("printing noop tracer")
            #print(tracer._noop_tracer)
            #print(tracer._noop_tracer.__dict__)
            # print("Printing what is in trace")
            # print(trace.__dict__)
            print(method_request)
            print("Request object")
            print(method_request.__dict__)
            print(type(method_request))
            print(_SPAN_KEY)
            #print(method_request.headers)
            # propagator = tracer.get_propagator()
            # print("printing propagator")
            # print(propagator)
            # span_ctx = tracer.extract(_SPAN_KEY)
            # print("Printing span context")
            # print(span_ctx)
            # print("Printing tags")
            #print(tags)
            #span_tags = {tags.SPAN_KIND: tags.SPAN_KIND_RPC_SERVER}

            # span_tags = {}
            # with tracer.start_span('format', child_of=span_ctx, tags=span_tags):
            #     hello_to = method_request.args.get('helloTo')
            #     print(hello_to) #'Hello, %s!' % hello_to
            
            span_processor = BatchSpanProcessor(exporter)
            print("print whats in span_processor")
            print(span_processor)
            print("print whats in span_processor object: span_processor.__dict__")
            print(span_processor.__dict__)

            print("print whats in span_processor.span_exporter")
            print(span_processor.span_exporter)
            print("print whats in span_processor.span_exporter.__dict__")
            print(span_processor.span_exporter.__dict__)
            trace.get_tracer_provider().add_span_processor(span_processor)

            print("print whats in span_processor.span_exporter._instrumentation_key")
            print(span_processor.span_exporter._instrumentation_key)
            print("print whats in span_processor.span_exporter.client")
            print(span_processor.span_exporter.client)            
            print("print whats in span_processor.span_exporter.client.__dict__")
            print(span_processor.span_exporter.client.__dict__)
            trace.get_tracer_provider().add_span_processor(span_processor)

            #with tracer.start_as_current_span("parent"):
            with tracer.start_span(name="root span") as root_span:
                ctx = baggage.set_baggage("foo", "bar")   
                print("Hello, World from IoT edge!")
                print("printing root span")
                print(root_span)
                print("Printing ctx")
                print(ctx)
            print(f"Global context baggage:{ baggage.get_all()}")
            print(f"Span context baggge: {baggage.get_all(context=ctx)}")
            print("Done exporter within the method")

            print(method_request)
            print("the data in the message received on input1 was ")
            print(method_request.name)
            print(method_request.payload)
            print("the whole method request is", method_request.__dict__)
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

        async def message_handler(message):
            print("In message handler")
            print(message)
            print("Message object")
            print(message.__dict__)
            print(type(message))
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
        module_client.on_message_received = message_handler
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