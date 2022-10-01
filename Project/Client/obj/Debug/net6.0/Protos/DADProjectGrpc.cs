// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Protos/DADProject.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

namespace DADProject {
  public static partial class ProjectService
  {
    static readonly string __ServiceName = "ProjectService";

    static void __Helper_SerializeMessage(global::Google.Protobuf.IMessage message, grpc::SerializationContext context)
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (message is global::Google.Protobuf.IBufferMessage)
      {
        context.SetPayloadLength(message.CalculateSize());
        global::Google.Protobuf.MessageExtensions.WriteTo(message, context.GetBufferWriter());
        context.Complete();
        return;
      }
      #endif
      context.Complete(global::Google.Protobuf.MessageExtensions.ToByteArray(message));
    }

    static class __Helper_MessageCache<T>
    {
      public static readonly bool IsBufferMessage = global::System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(global::Google.Protobuf.IBufferMessage)).IsAssignableFrom(typeof(T));
    }

    static T __Helper_DeserializeMessage<T>(grpc::DeserializationContext context, global::Google.Protobuf.MessageParser<T> parser) where T : global::Google.Protobuf.IMessage<T>
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (__Helper_MessageCache<T>.IsBufferMessage)
      {
        return parser.ParseFrom(context.PayloadAsReadOnlySequence());
      }
      #endif
      return parser.ParseFrom(context.PayloadAsNewBuffer());
    }

    static readonly grpc::Marshaller<global::DADProject.PerfectChannelRequest> __Marshaller_PerfectChannelRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DADProject.PerfectChannelRequest.Parser));
    static readonly grpc::Marshaller<global::DADProject.PerfectChannelReply> __Marshaller_PerfectChannelReply = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DADProject.PerfectChannelReply.Parser));

    static readonly grpc::Method<global::DADProject.PerfectChannelRequest, global::DADProject.PerfectChannelReply> __Method_Test = new grpc::Method<global::DADProject.PerfectChannelRequest, global::DADProject.PerfectChannelReply>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Test",
        __Marshaller_PerfectChannelRequest,
        __Marshaller_PerfectChannelReply);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::DADProject.DADProjectReflection.Descriptor.Services[0]; }
    }

    /// <summary>Client for ProjectService</summary>
    public partial class ProjectServiceClient : grpc::ClientBase<ProjectServiceClient>
    {
      /// <summary>Creates a new client for ProjectService</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public ProjectServiceClient(grpc::ChannelBase channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for ProjectService that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public ProjectServiceClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected ProjectServiceClient() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected ProjectServiceClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      public virtual global::DADProject.PerfectChannelReply Test(global::DADProject.PerfectChannelRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Test(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::DADProject.PerfectChannelReply Test(global::DADProject.PerfectChannelRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_Test, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::DADProject.PerfectChannelReply> TestAsync(global::DADProject.PerfectChannelRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return TestAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::DADProject.PerfectChannelReply> TestAsync(global::DADProject.PerfectChannelRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_Test, null, options, request);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override ProjectServiceClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new ProjectServiceClient(configuration);
      }
    }

  }
}
#endregion
