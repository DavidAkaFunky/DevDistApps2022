using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace DADProject;

public class BoneyInterceptor : Interceptor
{
	// private readonly ILogger logger;

	//public GlobalServerLoggerInterceptor(ILogger logger) {
	//    this.logger = logger;
	//}

	private int _id = 0;
	private int Id
	{
		set => _id = value;
		get => _id;
	}
	
	private int seq = 0;

	public BoneyInterceptor(int id)
	{
		Id = id;
	}

	// public BoneyInterceptor(string address)
	// {
	// 	this.address = address;
	// }

	public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
	{

		Metadata metadata = context.Options.Headers; // read original headers
		if (metadata == null) metadata = new Metadata();
				
		metadata.Add("id", $"{Id}");
		metadata.Add("seq", $"{seq++}");
		

		// create new context because original context is readonly
		ClientInterceptorContext<TRequest, TResponse> modifiedContext =
			new(context.Method, context.Host,
				new(metadata, context.Options.Deadline,
					context.Options.CancellationToken, context.Options.WriteOptions,
					context.Options.PropagationToken, context.Options.Credentials));
		Console.Write("calling server...");
		TResponse response = base.BlockingUnaryCall(request, modifiedContext, continuation);
		return response;
	}
}