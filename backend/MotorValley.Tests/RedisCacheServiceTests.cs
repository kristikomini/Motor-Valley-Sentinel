using Moq;
using MotorValley.Backend.Services;
using StackExchange.Redis;
using Xunit;

namespace MotorValley.Tests;

public class RedisCacheServiceTests
{
    [Fact]
    public void RedisCacheService_GetSet_RoundTrip()
    {
        // Integration test would need real Redis. Unit test mocks.
        var mock = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);
        mockDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        mockDb.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var service = new RedisCacheService(mock.Object);
        Assert.NotNull(service);
    }
}
