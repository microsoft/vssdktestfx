// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class CalculatorMock : ICalculator
{
    public ValueTask<double> AddAsync(double a, double b, CancellationToken cancellationToken) => new(a + b);
}
