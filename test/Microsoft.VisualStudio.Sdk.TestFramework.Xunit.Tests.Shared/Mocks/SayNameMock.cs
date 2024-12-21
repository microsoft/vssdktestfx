// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class SayNameMock : ISayName
{
    public ValueTask<string> SayName() => new("Daffy Duck");
}
