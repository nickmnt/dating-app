using System.Security.Claims;
using API.Controllers;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Test;

public class MessagesControllerTest
{
    [Fact]
    public async Task CreateMessage_ValidUsername_ReturnsMessageDto()
    {
        // Arrange
        var sourceUsername = "sourceUsername";
        var sourceUserId = 10;
        var recipientUsername = "testUser";
        var recipientUserId = 2;
        var createMessageDto = new CreateMessageDto
        {
            Content = "whatevermessage",
            RecipientUsername = recipientUsername
        };
        var sender = new AppUser { UserName = sourceUsername, Id = sourceUserId };
        var recipient = new AppUser { UserName = recipientUsername, Id = recipientUserId };
        var message = new Message
        {
            Sender = sender,
            Recipient = recipient,
            SenderUsername = sender.UserName,
            RecipientUsername = recipient.UserName,
            Content = createMessageDto.Content
        };
        


        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock.Setup(repo => repo.GetUserByUsernameAsync(sourceUsername))
            .ReturnsAsync(sender);
        userRepositoryMock.Setup(repo => repo.GetUserByUsernameAsync(recipientUsername))
            .ReturnsAsync(recipient);
        
        var messageRepositoryMock = new Mock<IMessageRepository>();
        messageRepositoryMock.Setup(repo => repo.AddMessage(message));

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(uow => uow.UserRepository).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(uow => uow.MessageRepository).Returns(messageRepositoryMock.Object);
        unitOfWorkMock.Setup(uow => uow.Complete()).ReturnsAsync(true);

        var mapper = new Mock<IMapper>();
        mapper.Setup(mapper => mapper.Map<MessageDto>(message)).Returns(new MessageDto {});
        
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, sourceUsername),
            new Claim(ClaimTypes.NameIdentifier, sourceUserId.ToString()),
            new Claim("custom-claim", "example claim value"),
        }, "mock"));

        var controller = new MessagesController(mapper.Object, unitOfWorkMock.Object);
        
        controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = user };

        // Act
        var result = await controller.CreateMessage(createMessageDto);

        // Assert
        var okResult = Assert.IsType<ActionResult<MessageDto>>(result);
        Assert.NotNull(okResult);
    }
    
        [Fact]
    public async Task CreateMessage_ToSelf_ReturnsBadRequest()
    {
        // Arrange
        var sourceUsername = "sourceUsername";
        var sourceUserId = 10;
        var recipientUsername = "sourceUsername";
        var recipientUserId = 10;
        var createMessageDto = new CreateMessageDto
        {
            Content = "whatevermessage",
            RecipientUsername = recipientUsername
        };
        var sender = new AppUser { UserName = sourceUsername, Id = sourceUserId };
        var recipient = new AppUser { UserName = recipientUsername, Id = recipientUserId };
        var message = new Message
        {
            Sender = sender,
            Recipient = recipient,
            SenderUsername = sender.UserName,
            RecipientUsername = recipient.UserName,
            Content = createMessageDto.Content
        };
        


        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock.Setup(repo => repo.GetUserByUsernameAsync(sourceUsername))
            .ReturnsAsync(sender);
        userRepositoryMock.Setup(repo => repo.GetUserByUsernameAsync(recipientUsername))
            .ReturnsAsync(recipient);
        
        var messageRepositoryMock = new Mock<IMessageRepository>();
        messageRepositoryMock.Setup(repo => repo.AddMessage(message));

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(uow => uow.UserRepository).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(uow => uow.MessageRepository).Returns(messageRepositoryMock.Object);
        unitOfWorkMock.Setup(uow => uow.Complete()).ReturnsAsync(true);

        var mapper = new Mock<IMapper>();
        mapper.Setup(mapper => mapper.Map<MessageDto>(message)).Returns(new MessageDto {});
        
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, sourceUsername.ToLower()),
            new Claim(ClaimTypes.NameIdentifier, sourceUserId.ToString()),
            new Claim("custom-claim", "example claim value"),
        }, "mock"));

        var controller = new MessagesController(mapper.Object, unitOfWorkMock.Object);
        
        controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = user };

        // Act
        var result = await controller.CreateMessage(createMessageDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("You cannot send messages to yourself", badRequestResult.Value);
    }
}