using System.Security.Claims;
using System.Text.Json;
using API.Controllers;
using API.Data;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Test;

public class LikesControllerTest
{
    [Fact]
    public async Task AddLike_InvalidUsername_ReturnsNotFoundResult()
    {
        // Arrange
        var username = "nonexistentUser";
        var sourceUserId = 10;

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock.Setup(repo => repo.GetUserByUsernameAsync(username))
            .ReturnsAsync((AppUser)null);

        var likesRepositoryMock = new Mock<ILikesRepository>();
        // setup for GetUserWithLikes and GetUserLike can be reused from the previous test
    
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(uow => uow.UserRepository).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(uow => uow.LikesRepository).Returns(likesRepositoryMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, "example name"),
            new Claim(ClaimTypes.NameIdentifier, sourceUserId.ToString()),
            new Claim("custom-claim", "example claim value"),
        }, "mock"));

        var controller = new LikesController(unitOfWorkMock.Object);

        controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = user };

        // Act
        var result = await controller.AddLike(username);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.NotNull(notFoundResult);
    }

    [Fact]
    public async Task AddLike_ValidUsername_ReturnsOkResult()
    {
        // Arrange
        var sourceUsername = "sourceUsername";
        var username = "testUser";
        var sourceUserId = 10;
        var likedUserId = 2;

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock.Setup(repo => repo.GetUserByUsernameAsync(username))
            .ReturnsAsync(new AppUser { UserName = username, Id = likedUserId });

        var likesRepositoryMock = new Mock<ILikesRepository>();
        likesRepositoryMock.Setup(repo => repo.GetUserWithLikes(sourceUserId))
            .ReturnsAsync(new AppUser {UserName = sourceUsername, LikedUsers = new List<UserLike>()});
        likesRepositoryMock.Setup(repo => repo.GetUserLike(sourceUserId, likedUserId))
            .ReturnsAsync((UserLike)null);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(uow => uow.UserRepository).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(uow => uow.LikesRepository).Returns(likesRepositoryMock.Object);
        unitOfWorkMock.Setup(uow => uow.Complete()).ReturnsAsync(true);

        
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, "example name"),
            new Claim(ClaimTypes.NameIdentifier, "10"),
            new Claim("custom-claim", "example claim value"),
        }, "mock"));

        var controller = new LikesController(unitOfWorkMock.Object);
        
        controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = user };

        // Act
        var result = await controller.AddLike(username);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.NotNull(okResult);
    }

}