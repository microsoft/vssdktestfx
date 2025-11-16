// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.ServiceHub.Framework;
using PolyType;
using StreamJsonRpc;

internal static partial class Descriptors
{
    internal static readonly ServiceRpcDescriptor Calculator = new ServiceJsonRpcDescriptor(
        new ServiceMoniker("Calculator", new Version(1, 0)),
        clientInterface: null,
        ServiceJsonRpcDescriptor.Formatters.MessagePack,
        ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader,
        new Nerdbank.Streams.MultiplexingStream.Options { ProtocolMajorVersion = 3 })
        .WithExceptionStrategy(ExceptionProcessing.ISerializable);

    internal static readonly ServiceRpcDescriptor Greet = new ServiceJsonRpcDescriptor(
        new ServiceMoniker("Greet", new Version(1, 0)),
        clientInterface: typeof(ISayName),
        ServiceJsonRpcDescriptor.Formatters.MessagePack,
        ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader,
        new Nerdbank.Streams.MultiplexingStream.Options { ProtocolMajorVersion = 3 })
        .WithExceptionStrategy(ExceptionProcessing.ISerializable);

    internal static readonly ServiceRpcDescriptor CalculatorPoly = new ServiceJsonRpcPolyTypeDescriptor(
        new ServiceMoniker("Calculator", new Version(1, 0)),
        clientInterface: null,
        ServiceJsonRpcPolyTypeDescriptor.Formatters.NerdbankMessagePack,
        ServiceJsonRpcPolyTypeDescriptor.MessageDelimiters.BigEndianInt32LengthHeader,
        new Nerdbank.Streams.MultiplexingStream.Options { ProtocolMajorVersion = 3 },
        Witness.GeneratedTypeShapeProvider)
        .WithRpcTargetMetadata(RpcTargetMetadata.FromShape(Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<ICalculator>()))
        .WithExceptionStrategy(ExceptionProcessing.ISerializable);

    internal static readonly ServiceRpcDescriptor GreetPoly = new ServiceJsonRpcPolyTypeDescriptor(
        new ServiceMoniker("Greet", new Version(1, 0)),
        clientInterface: typeof(ISayName),
        ServiceJsonRpcPolyTypeDescriptor.Formatters.NerdbankMessagePack,
        ServiceJsonRpcPolyTypeDescriptor.MessageDelimiters.BigEndianInt32LengthHeader,
        new Nerdbank.Streams.MultiplexingStream.Options { ProtocolMajorVersion = 3 },
        Witness.GeneratedTypeShapeProvider)
        .WithRpcTargetMetadata(RpcTargetMetadata.FromShape(Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<IGreet>()))
        .WithExceptionStrategy(ExceptionProcessing.ISerializable);

    [GenerateShapeFor<bool>]
    private partial class Witness;
}
