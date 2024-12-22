// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class BrokeredServiceTests4 : BrokeredServiceContractTestBase<IGreet, GreetMock, ISayName, SayNameMock>
{
    public BrokeredServiceTests4(ITestOutputHelper logger)
        : base(logger, Descriptors.Greet)
    {
    }

    [Fact]
    public async Task MakeGreeting()
    {
        Assert.Equal("Nice to meet you, Daffy Duck.", await this.ClientProxy.MakeGreeting(this.TimeoutToken));
    }

    [Fact]
    public async Task GreetingMade()
    {
        var name = await this.ClientCallback.SayName();
        await this.AssertEventRaisedAsync<string>(
            (p, h) => p.GreetingSent += h,
            (p, h) => p.GreetingSent -= h,
            s => s.SendGreeting($"Nice to meet you, {name}."),
            a => Assert.Equal("Nice to meet you, Daffy Duck.", a));
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
