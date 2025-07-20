using SwimmingTrackSystem.Context;
using SwimmingTrackSystem.Data;
using SwimmingTrackSystem.Data.Repositories;
using SwimmingTrackSystem.Models;

namespace GolfClubSystem.Data;

public class UnitOfWork : IDisposable
{
    public readonly MyDbContext Context;
    public GenericRepository<Product> ProductRepository { get; }
    public GenericRepository<Setting> SettingRepository { get; }
    public GenericRepository<Transaction> TransactionRepository { get; }

    public UnitOfWork()
    {
        Context = new AppDbContextFactory().CreateDbContext([]);
        ProductRepository = new GenericRepository<Product>(Context);
        SettingRepository = new GenericRepository<Setting>(Context);
        TransactionRepository = new GenericRepository<Transaction>(Context);
    }

    public async Task SaveAsync()
    {
        await Context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}