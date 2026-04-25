using Grpc.Core;
using Proton.Engine.AppHost.Grpc;

namespace Proton.Engine.AppHost.Services.Grpc;

public class GreeterService : Greeter.GreeterBase
{
    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        string name = string.IsNullOrWhiteSpace(request.Name)
            ? "foo"
            : request.Name;

        return new HelloReply
        {
            Message = $"Hello, {name}"
        };
    }
}

