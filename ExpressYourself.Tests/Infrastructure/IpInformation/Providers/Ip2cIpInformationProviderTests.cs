using ExpressYourself.Application.Errors;
using ExpressYourself.Application.Exceptions;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Domain.Enums;
using ExpressYourself.Gateway.Ip2c;
using ExpressYourself.Infrastructure.IpInformation.Providers;
using Moq;

namespace ExpressYourself.Tests.Infrastructure.IpInformation.Providers;

public sealed class Ip2cIpInformationProviderTests
{
    private const string Address = "1.2.3.4";
    private static readonly DateTimeOffset Now = new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IIp2cClient> _client = new();
    private readonly Mock<IIpAddressRepository> _ipAddresses = new();
    private readonly Mock<ICountryRepository> _countries = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<TimeProvider> _clock = new();

    public Ip2cIpInformationProviderTests()
    {
        _clock.Setup(c => c.GetUtcNow()).Returns(Now);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private Ip2cIpInformationProvider CreateSut() =>
        new(_client.Object, _ipAddresses.Object, _countries.Object, _unitOfWork.Object, _clock.Object);

    [Fact]
    public async Task Handle_Success_NewIp_ReturnsMappedDtoAndPersistsOnce()
    {
        // Arrange
        _client.Setup(c => c.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
        _ipAddresses.Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((IpAddress?)null);
        _countries.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Country?)null);

        IpAddress? added = null;
        _ipAddresses.Setup(r => r.Add(It.IsAny<IpAddress>())).Callback<IpAddress>(x => added = x);

        // Act
        IpInformationDto result = await CreateSut().GetIpInformationAsync(Address, CancellationToken.None);

        // Assert
        Assert.Equal(new IpInformationDto(Address, "GR", "GRC", "Greece"), result);
        _ipAddresses.Verify(r => r.Add(It.IsAny<IpAddress>()), Times.Once);
        _countries.Verify(r => r.Add(It.IsAny<Country>()), Times.Once);      // Country upsert default
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // atomic
        Assert.NotNull(added);
        Assert.Equal(IpStatus.Success, added!.Status);
        Assert.Equal("GR", added.CountryTwoLetterCode);
    }

    [Fact]
    public async Task Handle_Success_ExistingRowsDoesNotAddAgain()
    {
        // Arrange
        var existingIp = new IpAddress(Address);
        var existingCountry = new Country("GR", "GRC", "Greece");
        _client.Setup(c => c.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
        _ipAddresses.Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingIp);
        _countries.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(existingCountry);

        // Act
        await CreateSut().GetIpInformationAsync(Address, CancellationToken.None);

        // Assert
        _ipAddresses.Verify(r => r.Add(It.IsAny<IpAddress>()), Times.Never);
        _countries.Verify(r => r.Add(It.IsAny<Country>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(IpStatus.Success, existingIp.Status);
    }

    [Fact]
    public async Task Handle_Unknown_PersistsThenThrows()
    {
        // Arrange
        _client.Setup(c => c.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Unknown, null, null, null));
        _ipAddresses.Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((IpAddress?)null);
        IpAddress? added = null;
        _ipAddresses.Setup(r => r.Add(It.IsAny<IpAddress>())).Callback<IpAddress>(x => added = x);

        // Act + Assert
        await Assert.ThrowsAsync<UnknownIpAddressException>(
            () => CreateSut().GetIpInformationAsync(Address, CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(IpStatus.UnknownIp, added!.Status);
    }

    [Fact]
    public async Task Handle_Invalid_ThrowsWithoutPersisting()
    {
        // Arrange
        _client.Setup(c => c.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Invalid, null, null, null));

        // Act + Assert
        await Assert.ThrowsAsync<InvalidIpAddressException>(
            () => CreateSut().GetIpInformationAsync(Address, CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}