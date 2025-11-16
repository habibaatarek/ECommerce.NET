using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReviewRepository : Repository<Review>, IRepository<Review>
{
    private new readonly ApplicationDbContext _context;

    public ReviewRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<Review?> GetByIdAsync(Guid id)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public override async Task<IEnumerable<Review>> GetAllAsync()
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .ToListAsync();
    }

    public override async Task<IEnumerable<Review>> FindAsync(System.Linq.Expressions.Expression<Func<Review, bool>> predicate)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(predicate)
            .ToListAsync();
    }
}

