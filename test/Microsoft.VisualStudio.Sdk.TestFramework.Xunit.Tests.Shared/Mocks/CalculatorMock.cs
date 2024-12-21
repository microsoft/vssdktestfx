// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class CalculatorMock : ICalculator
{
    public event EventHandler<double>? SumFound;

    public event EventHandler? OperationComplete;

    public ValueTask<double> AddAsync(double a, double b, CancellationToken cancellationToken) => new(a + b);

    internal void RaiseSumFound(double value) => this.SumFound?.Invoke(this, value);

    internal void RaiseOperationComplete() => this.OperationComplete?.Invoke(this, EventArgs.Empty);
}
