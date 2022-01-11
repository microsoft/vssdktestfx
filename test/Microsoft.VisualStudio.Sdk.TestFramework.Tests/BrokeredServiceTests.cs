// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class BrokeredServiceTests : BrokeredServiceContractTestBase<ICalculator, CalculatorMock>
{
    public BrokeredServiceTests(ITestOutputHelper logger)
        : base(logger, Descriptors.Calculator)
    {
    }

    [Fact]
    public async Task AddAsync()
    {
        Assert.Equal(6.8, await this.ClientProxy.AddAsync(2.3, 4.5, this.TimeoutToken));
    }
}
