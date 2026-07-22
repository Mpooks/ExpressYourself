using System.Data;

namespace ExpressYourself.Infrastructure.Persistence.Abstraction
{
    public interface IDbConnectionFactory
    {
        IDbConnection InitializeConnection();
    }
}
