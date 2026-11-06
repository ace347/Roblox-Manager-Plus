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
    public async Task CreateAccountAsync_Throws_WhenUsernameIsEmpty()
    {
        var mockRepo = new Mock<IAccountRepository>();
        var service = new AccountService(mockRepo.Object, NullLogger<AccountService>.Instance);

        var act = async () => await service.CreateAccountAsync("", "Display", "Main", null, false);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAccountAsync_Throws_WhenDuplicateUsernameExists()
    {
        var mockRepo = new Mock<IAccountRepository>();
        mockRepo.Setup(r => r.GetByUsernameAsync("ExistingUser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { Username = "ExistingUser" });

        var service = new AccountService(mockRepo.Object, NullLogger<AccountService>.Instance);
        var act = async () => await service.CreateAccountAsync("ExistingUser", "Display", "Main", null, false);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateAccountAsync_AddsAccount_WhenValid()
    {
        var mockRepo = new Mock<IAccountRepository>();
        mockRepo.Setup(r => r.GetByUsernameAsync("NewUser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var service = new AccountService(mockRepo.Object, NullLogger<AccountService>.Instance);
        await service.CreateAccountAsync("NewUser", "MyDisplay", "Developer", "Notes", true);

        mockRepo.Verify(r => r.AddAsync(It.Is<Account>(a => a.Username == "NewUser" && a.IsDefault), It.IsAny<CancellationToken>()), Times.Once);
    }
}
