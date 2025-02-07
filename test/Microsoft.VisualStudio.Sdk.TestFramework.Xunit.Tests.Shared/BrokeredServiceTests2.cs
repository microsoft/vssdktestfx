// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class BrokeredServiceTests2 : BrokeredServiceContractTestBase<ICalculator, CalculatorMock>
{
    public BrokeredServiceTests2(ITestOutputHelper logger)
        : base(logger, Descriptors.Calculator)
    {
    }

    [Fact]
    public async Task AddAsync()
    {
        Assert.Equal(6.8, await this.ClientProxy.AddAsync(2.3, 4.5, this.TimeoutToken));
    }

    [Fact]
    public async Task SumFound()
    {
        await this.AssertEventRaisedAsync<double>(
            (p, h) => p.SumFound += h,
            (p, h) => p.SumFound -= h,
            s => s.RaiseSumFound(50),
            a => Assert.Equal(50, a));
    }

    [Fact]
    public async Task OperationComplete()
    {
        await this.AssertEventRaisedAsync(
            (p, h) => p.OperationComplete += h,
            (p, h) => p.OperationComplete -= h,
            s => s.RaiseOperationComplete());
    }
}
