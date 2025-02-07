﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public interface ICalculator
{
    event EventHandler<double> SumFound;

    event EventHandler OperationComplete;

    ValueTask<double> AddAsync(double a, double b, CancellationToken cancellationToken);
}
