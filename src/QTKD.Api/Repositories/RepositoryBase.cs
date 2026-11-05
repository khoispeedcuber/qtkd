using Microsoft.EntityFrameworkCore;
using Npgsql;
using QTKD.Api.Data;
using QTKD.Api.Exceptions;

namespace QTKD.Api.Repositories;

/// <summary>
/// Lớp cơ sở của Repository: giữ DbContext và gom bước lưu thay đổi.
/// Vi phạm ràng buộc duy nhất của PostgreSQL (mã 23505) được đổi thành <see cref="ConflictException"/>,
/// nên hai yêu cầu đồng thời cùng ghi một dữ liệu chỉ có một yêu cầu thành công.
/// </summary>
public abstract class RepositoryBase
{
    protected readonly QtkdDbContext Db;

    protected RepositoryBase(QtkdDbContext db) => Db = db;

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await Db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("Dữ liệu đã tồn tại hoặc đã được xử lý trước đó (trùng ràng buộc duy nhất).");
        }
    }
}
