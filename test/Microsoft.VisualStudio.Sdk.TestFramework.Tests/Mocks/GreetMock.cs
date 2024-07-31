// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static System.Reflection.BindingFlags;

public class GreetMock : IGreet, IMockServiceWithClientCallback
{
    public event EventHandler<string>? GreetingSent;

    public event EventHandler? OperationComplete;

    public object ClientCallback { get; set; } = new object();

    public async ValueTask<string> MakeGreeting(CancellationToken cancellationToken)
    {
#pragma warning disable CS8605
        var name = await (ValueTask<string>)typeof(ISayName).GetMethod("SayName")?.Invoke(this.ClientCallback, null);
#pragma warning restore CS8605
        return $"Nice to meet you, {name}.";
    }

    internal void SendGreeting(string greeting) => this.GreetingSent?.Invoke(this, greeting);

    internal void RaiseOperationComplete() => this.OperationComplete?.Invoke(this, EventArgs.Empty);
}
