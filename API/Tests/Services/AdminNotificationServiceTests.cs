using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using API.Hubs;
using API.Services;
using Core.DTOs.Order;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using NUnit.Framework;

namespace Tests.Services
{
    [TestFixture]
    public class AdminNotificationServiceTests
    {
        private Mock<IHubContext<NotificationHub>> _hubContext = null!;
        private Mock<IHubClients> _hubClients = null!;
        private Mock<IClientProxy> _clientProxy = null!;

        [SetUp]
        public void SetUp()
        {
            _hubContext = new Mock<IHubContext<NotificationHub>>();
            _hubClients = new Mock<IHubClients>();
            _clientProxy = new Mock<IClientProxy>();

            _hubContext.SetupGet(h => h.Clients).Returns(_hubClients.Object);
            _hubClients.Setup(c => c.Group("Admins")).Returns(_clientProxy.Object);
        }

        [Test]
        public async Task NotifyOrderPlacedAsync_Sends_To_Admins_Group_With_GetOrderDto_Payload()
        {
            // Arrange
            var service = new AdminNotificationService(_hubContext.Object);
            GetOrderDto dto = new GetOrderDto
            {
                Id = 123,
                SellerId = 88,
                SellerName = "sellerA",
                CustomerName = "cust",
                TotalAmount = 99.5m,
                OrderDate = DateTime.UtcNow,
                Status = Data.Models.Enums.OrderStatus.Pending,
            };

            object[]? capturedArgs = null;
            _clientProxy
                .Setup(p => p.SendCoreAsync(
                    It.Is<string>(m => m == "OrderPlaced"),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, object[], CancellationToken>((_, args, __) => capturedArgs = args)
                .Returns(Task.CompletedTask);

            // Act
            await service.NotifyOrderPlacedAsync(dto);

            // Assert
            _hubClients.Verify(c => c.Group("Admins"), Times.Once);
            _clientProxy.Verify(p => p.SendCoreAsync("OrderPlaced", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
            capturedArgs.Should().NotBeNull();
            capturedArgs!.Length.Should().Be(1);

            // Validate that the payload is the GetOrderDto instance with expected values
            var payload = capturedArgs[0];
            payload.Should().BeOfType<GetOrderDto>();
            var sentDto = (GetOrderDto)payload;
            sentDto.Should().BeSameAs(dto);
            sentDto.Id.Should().Be(123);
            sentDto.SellerId.Should().Be(88);
            sentDto.SellerName.Should().Be("sellerA");
            sentDto.CustomerName.Should().Be("cust");
            sentDto.TotalAmount.Should().Be(99.5m);
            sentDto.Status.ToString().Should().Be("Pending");
        }

        [Test]
        public void NotifyOrderPlacedAsync_Propagates_Hub_Errors()
        {
            // Arrange
            var service = new AdminNotificationService(_hubContext.Object);
            var dto = new GetOrderDto { Id = 1, SellerId = 2, SellerName = "s", CustomerName = "c", TotalAmount = 1m, OrderDate = DateTime.UtcNow, Status = Data.Models.Enums.OrderStatus.Pending };

            _clientProxy
                .Setup(p => p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("hub down"));

            // Act
            Func<Task> act = async () => await service.NotifyOrderPlacedAsync(dto);

            // Assert
            act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*hub down*");
        }
    }
}
