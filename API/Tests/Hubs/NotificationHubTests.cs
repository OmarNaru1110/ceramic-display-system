using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Hubs;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using NUnit.Framework;

namespace Tests.Hubs
{
    [TestFixture]
    public class NotificationHubTests
    {
        private class TestableHub : NotificationHub
        {
            public TestableHub() : base() { }
            public void SetContext(HubCallerContext context, IGroupManager groups)
            {
                // use protected Hub properties through reflection since Hub sets them internally
                typeof(Hub).GetProperty("Context")!.SetValue(this, context);
                typeof(Hub).GetProperty("Groups")!.SetValue(this, groups);
            }
        }

        private static HubCallerContext CreateContextWithRole(string? role)
        {
            var identity = new ClaimsIdentity();
            if (!string.IsNullOrEmpty(role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
                identity.AddClaim(new Claim("role", role));
            }
            var principal = new ClaimsPrincipal(identity);

            var mockContext = new Mock<HubCallerContext>();
            mockContext.SetupGet(c => c.User).Returns(principal);
            mockContext.SetupGet(c => c.ConnectionId).Returns("conn-1");
            return mockContext.Object;
        }

        [Test]
        public async Task OnConnectedAsync_Admin_User_Joins_Admins_Group()
        {
            // Arrange
            var hub = new TestableHub();
            var context = CreateContextWithRole("Admin");
            var groups = new Mock<IGroupManager>();

            groups.Setup(g => g.AddToGroupAsync("conn-1", "Admins", default)).Returns(Task.CompletedTask).Verifiable();

            hub.SetContext(context, groups.Object);

            // Act
            await hub.OnConnectedAsync();

            // Assert
            groups.Verify();
        }

        [Test]
        public async Task OnConnectedAsync_NonAdmin_User_Does_Not_Join_Group()
        {
            // Arrange
            var hub = new TestableHub();
            var context = CreateContextWithRole("Seller");
            var groups = new Mock<IGroupManager>();

            hub.SetContext(context, groups.Object);

            // Act
            await hub.OnConnectedAsync();

            // Assert
            groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }

        [Test]
        public async Task JoinAdminsGroup_Adds_Current_Connection_To_Group()
        {
            // Arrange
            var hub = new TestableHub();
            var context = CreateContextWithRole("Admin");
            var groups = new Mock<IGroupManager>();

            groups.Setup(g => g.AddToGroupAsync("conn-1", "Admins", default)).Returns(Task.CompletedTask).Verifiable();

            hub.SetContext(context, groups.Object);

            // Act
            await hub.JoinAdminsGroup();

            // Assert
            groups.Verify();
        }

        [Test]
        public async Task LeaveAdminsGroup_Removes_Current_Connection_From_Group()
        {
            // Arrange
            var hub = new TestableHub();
            var context = CreateContextWithRole("Admin");
            var groups = new Mock<IGroupManager>();

            groups.Setup(g => g.RemoveFromGroupAsync("conn-1", "Admins", default)).Returns(Task.CompletedTask).Verifiable();

            hub.SetContext(context, groups.Object);

            // Act
            await hub.LeaveAdminsGroup();

            // Assert
            groups.Verify();
        }

        [Test]
        public async Task OnConnectedAsync_NoUser_Does_Not_Join_Group()
        {
            // Arrange
            var hub = new TestableHub();
            var mockContext = new Mock<HubCallerContext>();
            mockContext.SetupGet(c => c.User).Returns((ClaimsPrincipal?)null);
            mockContext.SetupGet(c => c.ConnectionId).Returns("conn-2");
            var groups = new Mock<IGroupManager>();

            hub.SetContext(mockContext.Object, groups.Object);

            // Act
            await hub.OnConnectedAsync();

            // Assert
            groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }
    }
}
