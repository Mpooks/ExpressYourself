using ExpressYourself.Application.Behaviors;
using ExpressYourself.Application.Errors;
using ExpressYourself.Application.Exceptions;
using ExpressYourself.Application.Interfaces;
using MediatR;
using Moq;

namespace ExpressYourself.Tests.Application.Behaviors;

public sealed class ValidationBehaviorTests
{
    public sealed record FakeRequest(string Value) : IRequest<string>;

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<FakeRequest, string>(Array.Empty<IValidator<FakeRequest>>());
        var nextCalled = false;
        RequestHandlerDelegate<string> next = () => { nextCalled = true; return Task.FromResult("ok"); };

        string result = await behavior.Handle(new FakeRequest("x"), next, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_ValidatorPasses_CallsNext()
    {
        var validator = new Mock<IValidator<FakeRequest>>();
        var behavior = new ValidationBehavior<FakeRequest, string>(new[] { validator.Object });
        RequestHandlerDelegate<string> next = () => Task.FromResult("ok");

        string result = await behavior.Handle(new FakeRequest("x"), next, CancellationToken.None);

        validator.Verify(v => v.Validate(It.IsAny<FakeRequest>()), Times.Once);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_ValidatorThrows_ShortCircuits_NextNeverCalled()
    {
        var validator = new Mock<IValidator<FakeRequest>>();
        validator.Setup(v => v.Validate(It.IsAny<FakeRequest>()))
                 .Throws(new InvalidIpAddressException("bad", "bad"));
        var behavior = new ValidationBehavior<FakeRequest, string>(new[] { validator.Object });
        var nextCalled = false;
        RequestHandlerDelegate<string> next = () => { nextCalled = true; return Task.FromResult("ok"); };

        await Assert.ThrowsAsync<InvalidIpAddressException>(
            () => behavior.Handle(new FakeRequest("x"), next, CancellationToken.None));

        Assert.False(nextCalled);
    }
}