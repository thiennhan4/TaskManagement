using Microsoft.EntityFrameworkCore;
using TaskHub.Infrastructure.Data;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;

namespace TaskHub.Infrastructure.Repositories;

public class BoardListRepository : IBoardListRepository
{
    private readonly AppDbContext _context;

    public BoardListRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BoardList>> GetListsByBoardIdAsync(Guid boardId, CancellationToken ct = default)
    {
        return await _context.Lists
            .Where(l => l.BoardId == boardId)
            .OrderBy(l => l.Position)
            .Include(l => l.Tasks)
            .ToListAsync(ct);
    }

    public async Task<BoardList?> GetListByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Lists
            .Include(l => l.Board)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<BoardList> CreateListAsync(BoardList list, CancellationToken ct = default)
    {
        _context.Lists.Add(list);
        await _context.SaveChangesAsync(ct);
        return list;
    }

    public async Task UpdateListAsync(BoardList list, CancellationToken ct = default)
    {
        _context.Entry(list).State = EntityState.Modified;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteListAsync(Guid id, CancellationToken ct = default)
    {
        var list = await _context.Lists.FindAsync(new object[] { id }, ct);
        if (list != null)
        {
            _context.Lists.Remove(list);
            await _context.SaveChangesAsync(ct);
        }
    }
}




