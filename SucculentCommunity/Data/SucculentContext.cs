using Microsoft.EntityFrameworkCore;
using SucculentCommunity.Models;
using System.Linq; // 🌟 記得要有這個，才能用後面的 LINQ 語法

namespace SucculentCommunity.Data
{
    public class SucculentContext : DbContext
    {
        public SucculentContext(DbContextOptions<SucculentContext> options) : base(options)
        {
        }

        public DbSet<Member> Members { get; set; }
        public DbSet<Species> Species { get; set; }
        public DbSet<Plant> Plants { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        // 🌟 封印死亡三角的終極指令：將所有外鍵預設改為「限制刪除 (Restrict)」
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var cascades = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

            foreach (var fk in cascades)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}