using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CartItemRepository : Repository<CartItem>, IRepository<CartItem>
{
    private new readonly ApplicationDbContext _context;

    public CartItemRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<CartItem?> GetByIdAsync(Guid id)
    {
        return await _context.CartItems
            .Include(ci => ci.User)
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == id);
    }

    public override async Task<IEnumerable<CartItem>> GetAllAsync()
    {
        return await _context.CartItems
            .Include(ci => ci.User)
            .Include(ci => ci.Product)
            .ToListAsync();
    }

    public override async Task<IEnumerable<CartItem>> FindAsync(System.Linq.Expressions.Expression<Func<CartItem, bool>> predicate)
    {
        return await _context.CartItems
            .Include(ci => ci.User)
            .Include(ci => ci.Product)
            .Where(predicate)
            .ToListAsync();
    }
}

