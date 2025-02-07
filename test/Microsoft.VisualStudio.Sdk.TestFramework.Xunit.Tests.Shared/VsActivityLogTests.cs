// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS

using Microsoft.VisualStudio.Sdk.TestFramework.Mocks;

[Collection(MockedVS.Collection)]
public class VsActivityLogTests
{
    private readonly ITestOutputHelper logger;

    public VsActivityLogTests(GlobalServiceProvider sp, ITestOutputHelper logger)
    {
        sp.Reset();
        this.logger = logger;
    }

    [Fact]
    public void NoForwarder()
    {
        // Does nothing, but make sure it doesn't throw.
        ActivityLog.LogWarning("my source", "my message");
    }

    [Fact]
    public void WithForwarder()
    {
        MockVsActivityLog mockLogger = ServiceProvider.GlobalProvider.GetService<SVsActivityLog, MockVsActivityLog>();
        mockLogger.ForwardTo = new MyLogger();
        Assert.Throws<NotImplementedException>(() => ActivityLog.LogInformation("my source", "my message"));
    }

    [Fact]
    public void WithXunitAdapter()
    {
        MockVsActivityLog mockLogger = ServiceProvider.GlobalProvider.GetService<SVsActivityLog, MockVsActivityLog>();
        mockLogger.ForwardTo = new MockVsActivityLogXunitAdapter(this.logger);
        ActivityLog.LogInformation("my source", "my message");
        ActivityLog.LogError("my source", "my message");
    }

    private class MyLogger : IVsActivityLog
    {
        public int LogEntry(uint actType, string pszSource, string pszDescription)
        {
            throw new NotImplementedException();
        }

        public int LogEntryGuid(uint actType, string pszSource, string pszDescription, Guid guid)
        {
            throw new NotImplementedException();
        }

        public int LogEntryHr(uint actType, string pszSource, string pszDescription, int hr)
        {
            throw new NotImplementedException();
        }

        public int LogEntryGuidHr(uint actType, string pszSource, string pszDescription, Guid guid, int hr)
        {
            throw new NotImplementedException();
        }

        public int LogEntryPath(uint actType, string pszSource, string pszDescription, string pszPath)
        {
            throw new NotImplementedException();
        }

        public int LogEntryGuidPath(uint actType, string pszSource, string pszDescription, Guid guid, string pszPath)
        {
            throw new NotImplementedException();
        }

        public int LogEntryHrPath(uint actType, string pszSource, string pszDescription, int hr, string pszPath)
        {
            throw new NotImplementedException();
        }

        public int LogEntryGuidHrPath(uint actType, string pszSource, string pszDescription, Guid guid, int hr, string pszPath)
        {
            throw new NotImplementedException();
        }
    }
}

#endif
