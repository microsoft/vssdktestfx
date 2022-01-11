// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public interface ICalculator
{
    ValueTask<double> AddAsync(double a, double b, CancellationToken cancellationToken);
}
