using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Exceptions;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Domain.Enums;
using ExpressYourself.Infrastructure.IpInformation.Providers;
using Moq;

namespace ExpressYourself.Tests.Infrastructure.IpInformation.Providers;

public sealed class Ip2cIpInformationProviderTests
{
    private const string Address = "1.2.3.4";

    private static readonly DateTimeOffset Now =
        new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);

    private static readonly Ip2cLookupResult SuccessfulResult =
        new(Ip2cLookupStatus.Success, "GR", "GRC", "Greece");

    private readonly Mock<IIp2cClient> _client = new();
    private readonly Mock<IIpAddressRepository> _ipAddresses = new();
    private readonly Mock<ICountryRepository> _countries = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<TimeProvider> _clock = new();
    private readonly Mock<IIpInformationCache> _cache = new();

    public Ip2cIpInformationProviderTests()
    {
        _clock.Setup(c => c.GetUtcNow()).Returns(Now);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _cache
            .Setup(c => c.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_Success_NewIp_ReturnsMappedDtoPersistsAndInvalidates()
    {
        _client
            .Setup(c => c.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessfulResult);

        _ipAddresses
            .Setup(r => r.GetByAddressAsync(Address,It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpAddress?)null);

        _countries
            .Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Country?)null);

        IpAddress? addedIp = null;

        _ipAddresses
            .Setup(r => r.Add(It.IsAny<IpAddress>()))
            .Callback<IpAddress>(ip => addedIp = ip);

        var sut = CreateSut();

        IpInformationDto result = await sut.GetIpInformationAsync(Address, CancellationToken.None);

        Assert.Equal(
            new IpInformationDto(Address, "GR", "GRC", "Greece"),
            result);

        Assert.NotNull(addedIp);
        Assert.Equal(IpStatus.Success, addedIp!.Status);
        Assert.Equal("GR", addedIp.CountryTwoLetterCode);

        _ipAddresses.Verify(r => r.Add(It.IsAny<IpAddress>()), Times.Once);

        _countries.Verify(r => r.Add(It.IsAny<Country>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(CancellationToken.None),Times.Once);

        _cache.Verify(c => c.RemoveAsync(Address, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_ExistingPendingIp_UpdatesAndInvalidates()
    {
        var existingIp = new IpAddress(Address);
        var existingCountry = new Country("GR", "GRC", "Greece");

        SetupSuccessfulLookup();

        _ipAddresses
            .Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIp);

        _countries
            .Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCountry);

        var sut = CreateSut();

        await sut.GetIpInformationAsync(Address, CancellationToken.None);

        Assert.Equal(IpStatus.Success, existingIp.Status);
        Assert.Equal("GR", existingIp.CountryTwoLetterCode);

        _ipAddresses.Verify(r => r.Add(It.IsAny<IpAddress>()), Times.Never);

        _countries.Verify(r => r.Add(It.IsAny<Country>()),Times.Never);

        _cache.Verify(c => c.RemoveAsync(Address, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_UnchangedIp_DoesNotInvalidate()
    {
        var existingIp = new IpAddress(Address);
        existingIp.SetCountry("GR", Now.AddHours(-1));

        var existingCountry = new Country("GR", "GRC", "Greece");

        SetupSuccessfulLookup();

        _ipAddresses
            .Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIp);

        _countries
            .Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCountry);

        var sut = CreateSut();

        await sut.GetIpInformationAsync(Address, CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(CancellationToken.None), Times.Once);

        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Success_ChangedCountryCode_InvalidatesAddress()
    {
        var existingIp = new IpAddress(Address);
        existingIp.SetCountry("US", Now.AddHours(-1));

        var country = new Country("GR", "GRC", "Greece");

        SetupSuccessfulLookup();

        _ipAddresses
            .Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIp);

        _countries
            .Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(country);

        var sut = CreateSut();

        await sut.GetIpInformationAsync(Address, CancellationToken.None);

        Assert.Equal("GR", existingIp.CountryTwoLetterCode);

        _cache.Verify(c => c.RemoveAsync(Address, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_Unknown_NewIp_PersistsAndInvalidates()
    {
        var unknownResult = new Ip2cLookupResult(Ip2cLookupStatus.Unknown, null, null, null);

        _client
            .Setup(c => c.GetIpInformationAsync( Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unknownResult);

        _ipAddresses
            .Setup(r => r.GetByAddressAsync( Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpAddress?)null);

        IpAddress? addedIp = null;

        _ipAddresses
            .Setup(r => r.Add(It.IsAny<IpAddress>()))
            .Callback<IpAddress>(ip => addedIp = ip);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnknownIpAddressException>(() =>
            sut.GetIpInformationAsync(Address, CancellationToken.None));

        Assert.NotNull(addedIp);
        Assert.Equal(IpStatus.UnknownIp, addedIp!.Status);

        _unitOfWork.Verify(u => u.SaveChangesAsync(CancellationToken.None), Times.Once);

        _cache.Verify(c => c.RemoveAsync(Address, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_Unknown_UnchangedIp_DoesNotInvalidate()
    {
        var existingIp = new IpAddress(Address);
        existingIp.MarkAsUnknown(Now.AddHours(-1));

        var unknownResult = new Ip2cLookupResult(Ip2cLookupStatus.Unknown, null, null, null);

        _client
            .Setup(c => c.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unknownResult);

        _ipAddresses
            .Setup(r => r.GetByAddressAsync( Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIp);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnknownIpAddressException>(() =>
            sut.GetIpInformationAsync(Address,CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(CancellationToken.None), Times.Once);

        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Invalid_DoesNotPersistOrInvalidate()
    {
        var invalidResult = new Ip2cLookupResult(Ip2cLookupStatus.Invalid, null, null, null);

        _client
            .Setup(c => c.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidResult);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidIpAddressException>(() =>
            sut.GetIpInformationAsync(Address, CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),Times.Never);

        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SaveFails_DoesNotInvalidate()
    {
        SetupSuccessfulLookup();

        _ipAddresses
            .Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpAddress?)null);

        _countries
            .Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Country?)null);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database write failed."));

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetIpInformationAsync(Address, CancellationToken.None));

        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(),It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Success_ChangedCountryMetadata_InvalidatesCountryAddresses()
    {
        const string secondAddress = "5.6.7.8";

        var existingIp = new IpAddress(Address);
        existingIp.SetCountry("GR", Now.AddHours(-1));

        var existingCountry = new Country("GR", "GRE", "Old Greece");

        SetupSuccessfulLookup();

        _ipAddresses
            .Setup(r => r.GetByAddressAsync(Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIp);

        _countries
            .Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCountry);

        _ipAddresses
            .Setup(r => r.GetAddressesByCountryCodeAsync( "GR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Address, secondAddress });

        var sut = CreateSut();

        await sut.GetIpInformationAsync(Address, CancellationToken.None);

        Assert.Equal("GRC", existingCountry.ThreeLetterCode);
        Assert.Equal("Greece", existingCountry.CountryName);

        _ipAddresses.Verify(
            r => r.GetAddressesByCountryCodeAsync("GR", CancellationToken.None), Times.Once);

        _cache.Verify(c => c.RemoveAsync(Address, CancellationToken.None), Times.Once);

        _cache.Verify(c => c.RemoveAsync(secondAddress, CancellationToken.None), Times.Once);

        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), CancellationToken.None), Times.Exactly(2));
    }

    private void SetupSuccessfulLookup()
    {
        _client
            .Setup(c => c.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessfulResult);
    }

    private Ip2cIpInformationProvider CreateSut() => new(_client.Object, _ipAddresses.Object, _countries.Object, _unitOfWork.Object, _clock.Object, _cache.Object);
}