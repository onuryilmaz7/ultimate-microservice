using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventBus.UnitTest;

[TestClass]
public class EventBusTests
{
    private ServiceCollection _services;
    

    public EventBusTests()
    {
        _services = new ServiceCollection();
        _services.AddLogging(configure => configure.AddConsole());
    }
    
    [TestMethod]
    public void TestMethod1()
    {
        var sp = _services.BuildServiceProvider();
    }
}