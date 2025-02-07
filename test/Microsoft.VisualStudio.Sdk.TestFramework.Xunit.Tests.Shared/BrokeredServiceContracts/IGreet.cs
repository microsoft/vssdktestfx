﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public interface IGreet
{
    event EventHandler<string> GreetingSent;

    event EventHandler OperationComplete;

    ValueTask<string> MakeGreeting(CancellationToken cancellationToken);
}
