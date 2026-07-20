using ExpressYourself.Application.Infrastructure.Persistence;

namespace ExpressYourself.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly ExpressYourselfDbContext _context;

    public UnitOfWork(ExpressYourselfDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
      
        return await _context.SaveChangesAsync(cancellationToken);
    }
}