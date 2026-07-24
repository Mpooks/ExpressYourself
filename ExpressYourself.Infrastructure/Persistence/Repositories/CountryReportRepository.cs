using Dapper;
using ExpressYourself.Application.Features.CountryReports.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Infrastructure.Persistence.Abstraction;
using ExpressYourself.Infrastructure.Persistence.Context;
using System.Data;

namespace ExpressYourself.Infrastructure.Persistence.Repositories;

public sealed class CountryReportRepository : ICountryReportRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public CountryReportRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    private const string PartialSql =
        """
        SELECT c.CountryName AS CountryName, 
        COUNT(*) AS AddressesCount, 
        MAX(ip.LastUpdatedAtUtc) AS LastAddressUpdated
        FROM IpAddresses ip
        INNER JOIN Countries c ON c.TwoLetterCode = ip.CountryTwoLetterCode
        """;

    private const string AdditionalSql = 
        """
        GROUP BY c.CountryName
        ORDER BY c.CountryName
        """;

    public async Task<IReadOnlyList<CountryReportDto>> GetAllAsync(IReadOnlyList<string>? codes, CancellationToken cancellationToken)
    {
        string filter = (codes is { Count: > 0 }) ? " WHERE c.TwoLetterCode IN @Codes " : " ";
        string sql = PartialSql + filter + AdditionalSql;
        using IDbConnection connection = _dbConnectionFactory.InitializeConnection();
        var command = new CommandDefinition(sql, new { Codes = codes }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<CountryReportDto>(command);
        
        return rows.ToList();
    }

}
