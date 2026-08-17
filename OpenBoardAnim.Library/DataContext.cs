using Microsoft.EntityFrameworkCore;

namespace OpenBoardAnim.Library
{
    public class DataContext : DbContext
    {
        public string DbPath { get; }
        public DbSet<GraphicEntity> Graphics { get; set; }
        public DbSet<ProjectEntity> Projects { get; set; }
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DataContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = Path.Join(path, "OpenBoardAnim.db");
        }

        // Default Timeout is how long ADO.NET retries against SQLITE_BUSY before giving up -
        // guards against a transient lock (AV scan, OneDrive sync) surfacing as an unhandled
        // SqliteException instead of just waiting briefly and succeeding.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath};Default Timeout=15");
    }
}
