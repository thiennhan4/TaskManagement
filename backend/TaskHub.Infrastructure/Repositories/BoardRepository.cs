using Microsoft.EntityFrameworkCore;
using TaskHub.Infrastructure.Data;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;

namespace TaskHub.Infrastructure.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly AppDbContext _context;

    public BoardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Board>> GetBoardsByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Boards
            .Where(b => b.OwnerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Board>> GetBoardsByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _context.Boards
            .Where(b => b.ProjectId == projectId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Board?> GetBoardByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Boards
            .Include(b => b.Lists)
            .ThenInclude(l => l.Tasks)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Board> CreateBoardAsync(Board board, CancellationToken ct = default)
    {
        _context.Boards.Add(board);
        await _context.SaveChangesAsync(ct);
        return board;
    }

    public async Task UpdateBoardAsync(Board board, CancellationToken ct = default)
    {
        _context.Entry(board).State = EntityState.Modified;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteBoardAsync(Guid id, CancellationToken ct = default)
    {
        var board = await _context.Boards.FindAsync(new object[] { id }, ct);
        if (board != null)
        {
            _context.Boards.Remove(board);
            await _context.SaveChangesAsync(ct);
        }
    }
}




