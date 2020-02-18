# gRPC + Polly + Jaeger C# Example

Example of how to run gRPC, Polly and Jaeger in a single app.

The app will bounce a request between two services (lefty and righty). Randomly
causing exceptions, randomly bouncing it back, or else just stopping.

gRPC is used to communicate between the services. It is great, you should try
it. Polly is used to make retries and exception handling work nicely. Jaeger is
used to monitor and visualize the flow of the data.

I think that this example illustrates why you might wanna consider any of these
libraries for any project!

## Dependencies

* Dotnet Core
* Make
* Python
* Docker & docker-compose


## How to?

`make build`

Builds the application. Run this before anything else.

`make jaeger`

Will start jaeger using docker. Run in separate terminal.

`make stats`

Opens the Jaeger UI in a browser.

`make lefty`

Starts one of the services. Run in separate terminal.

`make righty`

Starts one of the services. Run in separate terminal.

`make pinger`

Initiates a request that will bounce between two services.
