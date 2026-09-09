using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ReceiptIqDbContext(DbContextOptions<ReceiptIqDbContext> options) : DbContext(options)
{
}
