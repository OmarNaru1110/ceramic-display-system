using NUnit.Framework;
using Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Tests.Services
{
    [TestFixture]
    public class ConfigurationTests : TestBase
    {
        [Test]
        public void Configuration_RefreshTokenDurationInDays_ShouldReturnCorrectValue()
        {
            // Act
            var refreshTokenDuration = Configuration.GetValue<int>("RefreshTokenDurationInDays");

            // Assert
            refreshTokenDuration.Should().Be(7);
        }

        [Test]
        public void Configuration_RefreshTokenDurationInDays_ShouldNotBeNull()
        {
            // Act
            var refreshTokenDurationString = Configuration["RefreshTokenDurationInDays"];

            // Assert
            refreshTokenDurationString.Should().NotBeNull();
            refreshTokenDurationString.Should().Be("7");
        }

        [Test]
        public void Configuration_GetValue_ShouldHandleInvalidKeyGracefully()
        {
            // Act
            var invalidConfigValue = Configuration.GetValue<int>("NonExistentKey");

            // Assert
            invalidConfigValue.Should().Be(0); // Default value for int
        }
    }
}