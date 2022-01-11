// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.ServiceHub.Framework;

internal static class Descriptors
{
    internal static readonly ServiceRpcDescriptor Calculator = new ServiceJsonRpcDescriptor(
        new ServiceMoniker("Calculator", new Version(1, 0)),
        clientInterface: null,
        ServiceJsonRpcDescriptor.Formatters.MessagePack,
        ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader,
        new Nerdbank.Streams.MultiplexingStream.Options { ProtocolMajorVersion = 3 })
        .WithExceptionStrategy(StreamJsonRpc.ExceptionProcessing.ISerializable);
}
