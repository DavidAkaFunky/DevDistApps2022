using System;
using Grpc.Core.Interceptors;

namespace DADProject {

	internal class BoneyInterceptor : Interceptor
	{
		// private readonly ILogger logger;

		//public GlobalServerLoggerInterceptor(ILogger logger) {
		//    this.logger = logger;
		//}

		public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
		{

			Metadata metadata = context.Options.Headers; // read original headers
			if (metadata == null)
				metadata = new Metadata();
			metadata.Add("dad", "dad-value"); // add the additional metadata

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
}