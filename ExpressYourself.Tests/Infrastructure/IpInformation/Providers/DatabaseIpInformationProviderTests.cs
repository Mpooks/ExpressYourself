using ExpressYourself.Application.Exceptions;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Infrastructure.IpInformation.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExpressYourself.Tests.Infrastructure.IpInformation.Providers;

public sealed class DatabaseIpInformationProviderTests
{
    private const string Address = "1.2.3.4";

    private readonly Mock<IIpAddressRepository> _ipAddresses = new();
    private readonly Mock<ICountryRepository> _countries = new();
    private readonly Mock<IIpInformationProvider> _inner = new();

    private DatabaseIpInformationProvider CreateSut() =>
        new(_ipAddresses.Object, _countries.Object, _inner.Object,
            NullLogger<DatabaseIpInformationProvider>.Instance);

    private static IpAddress SuccessfulIp()
    {
        var ip = new IpAddress(Address);
        ip.SetCountry("GR", DateTimeOffset.UtcNow);
        return ip;
    }

    [Fact]
    public async Task Handle_SuccessHit_ReturnsDtoFromDatabaseWithNoExternalCall()
    {
        
        _ipAddresses.Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(SuccessfulIp());
        _countries.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new Country("GR", "GRC", "Greece"));

        
        IpInformationDto result = await CreateSut().GetIpInformationAsync(Address, CancellationToken.None);

        
        Assert.Equal(new IpInformationDto(Address, "GR", "GRC", "Greece"), result);
        _inner.Verify(p => p.GetIpInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Miss_DelegatesToInnerOnce()
    {
        
        var expected = new IpInformationDto(Address, "GR", "GRC", "Greece");
        _ipAddresses.Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((IpAddress?)null);
        _inner.Setup(p => p.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
              .ReturnsAsync(expected);

        
        IpInformationDto result = await CreateSut().GetIpInformationAsync(Address, CancellationToken.None);

        
        Assert.Equal(expected, result);
        _inner.Verify(p => p.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_KnownUnknown_ThrowsWithoutExternalCall()
    {
        
        var ip = new IpAddress(Address);
        ip.MarkAsUnknown(DateTimeOffset.UtcNow);
        _ipAddresses.Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ip);

        
        await Assert.ThrowsAsync<UnknownIpAddressException>(
            () => CreateSut().GetIpInformationAsync(Address, CancellationToken.None));
        _inner.Verify(p => p.GetIpInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SuccessHitButMissingCountryRow_FallsThroughToInner()
    {
        
        var expected = new IpInformationDto(Address, "GR", "GRC", "Greece");
        _ipAddresses.Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(SuccessfulIp());
        _countries.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Country?)null);
        _inner.Setup(p => p.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
              .ReturnsAsync(expected);

        
        IpInformationDto result = await CreateSut().GetIpInformationAsync(Address, CancellationToken.None);

        
        Assert.Equal(expected, result);
        _inner.Verify(p => p.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()), Times.Once);
    }
}