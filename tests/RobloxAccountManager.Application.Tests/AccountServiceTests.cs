using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;
using Xunit;

namespace RobloxAccountManager.Application.Tests;

public class AccountServiceTests
{
    [Fact]
    public async Task GetAccountsAsync_CallsRepositoryGetAll()
    {
        var mockRepo = new Mock<IAccountRepository>();
        var sampleList = new List<Account>
        {
            new() { Username = "Player1" }
        };
        mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleList);

        var service = new AccountService(mockRepo.Object, NullLogger<AccountService>.Instance);
        var result = await service.GetAccountsAsync();

        result.Should().HaveCount(1);
        result[0].Username.Should().Be("Player1");
        mockRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
