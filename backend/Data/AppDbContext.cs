using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

// «Какие таблицы видит программа». Каждый DbSet — одна таблица PostgreSQL.
// Имена таблиц, столбцов, ключей, индексов и ограничений заданы явно и совпадают с db/schema.sql (ЛР9):
// по соглашениям EF получились бы "WorkOrders"."EquipmentId", а в базе нужно work_orders.equipment_id.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Area> Areas => Set<Area>();

    public DbSet<Equipment> Equipment => Set<Equipment>();

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------------------------------------------------------------- areas
        modelBuilder.Entity<Area>(e =>
        {
            e.ToTable("areas");
            e.HasKey(a => a.Id).HasName("areas_pkey");
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.Name).HasColumnName("name").HasMaxLength(60).IsRequired();
            e.HasIndex(a => a.Name).IsUnique().HasDatabaseName("areas_name_key");

            e.HasData(ReferenceData.Areas);
        });

        // ------------------------------------------------------------ equipment
        modelBuilder.Entity<Equipment>(e =>
        {
            e.ToTable("equipment");
            e.HasKey(x => x.Id).HasName("equipment_pkey");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.InventoryNumber).HasColumnName("inventory_number").HasMaxLength(20).IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.AreaId).HasColumnName("area_id");

            e.HasIndex(x => x.InventoryNumber).IsUnique().HasDatabaseName("equipment_inventory_number_key");
            // зачем: JOIN оборудования с участком
            e.HasIndex(x => x.AreaId).HasDatabaseName("equipment_area_id_idx");

            e.HasOne(x => x.Area).WithMany()
                .HasForeignKey(x => x.AreaId)
                .HasConstraintName("equipment_area_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);

            e.HasData(ReferenceData.Equipment);
        });

        // ---------------------------------------------------------------- users
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("users", t => t.HasCheckConstraint("users_role_check",
                $"role IN ({SqlList(UserRoles.All)})"));
            e.HasKey(u => u.Id).HasName("users_pkey");
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Login).HasColumnName("login").HasMaxLength(40).IsRequired();
            e.Property(u => u.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
            e.Property(u => u.Role).HasColumnName("role").HasMaxLength(16).IsRequired();
            e.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(200);

            e.HasIndex(u => u.Login).IsUnique().HasDatabaseName("users_login_key");

            e.HasData(ReferenceData.Users);
        });

        // ---------------------------------------------------------- work_orders
        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.ToTable("work_orders", t =>
            {
                t.HasCheckConstraint("work_orders_title_check",
                    "char_length(btrim(title)) BETWEEN 5 AND 80");
                t.HasCheckConstraint("work_orders_description_check",
                    "char_length(btrim(description)) BETWEEN 10 AND 500");
                // Только четыре кода статуса, как в API и на клиенте.
                t.HasCheckConstraint("work_orders_status_check",
                    $"status IN ({SqlList(WorkOrderStatuses.All)})");
                // Правило процесса ЛР8: «в ремонт» и «выполнена» только с назначенным исполнителем.
                t.HasCheckConstraint("work_orders_work_needs_assignee_chk",
                    "status IN ('New', 'Cancelled') OR assignee_user_id IS NOT NULL");
            });

            e.HasKey(w => w.Id).HasName("work_orders_pkey");
            e.Property(w => w.Id).HasColumnName("id");
            e.Property(w => w.Number).HasColumnName("number").HasMaxLength(16).IsRequired();
            e.Property(w => w.Title).HasColumnName("title").HasMaxLength(80).IsRequired();
            e.Property(w => w.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            e.Property(w => w.Status).HasColumnName("status").HasMaxLength(16).IsRequired()
                .HasDefaultValue(WorkOrderStatuses.New);
            e.Property(w => w.EquipmentId).HasColumnName("equipment_id");
            e.Property(w => w.CreatedByUserId).HasColumnName("created_by_user_id");
            e.Property(w => w.AssigneeUserId).HasColumnName("assignee_user_id");
            e.Property(w => w.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.Property(w => w.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

            // Человеческий номер уникален: повтор → ошибка 23505, сервис превращает её в 409.
            e.HasIndex(w => w.Number).IsUnique().HasDatabaseName("work_orders_number_key");
            // зачем: фильтр списка по статусу и сводка начальника цеха (ФТ-11)
            e.HasIndex(w => w.Status).HasDatabaseName("work_orders_status_idx");
            // зачем: фильтр по станку и JOIN заявок с оборудованием
            e.HasIndex(w => w.EquipmentId).HasDatabaseName("work_orders_equipment_id_idx");
            // зачем: «Мои заявки» слесаря (ФТ-12)
            e.HasIndex(w => w.AssigneeUserId).HasDatabaseName("work_orders_assignee_user_id_idx");
            // зачем: сортировка «новые сверху» (ORDER BY created_at DESC)
            e.HasIndex(w => w.CreatedAt).IsDescending().HasDatabaseName("work_orders_created_at_idx");
            // EF сам добавляет индекс на каждый FK; в schema.sql его не было — даём понятное имя.
            e.HasIndex(w => w.CreatedByUserId).HasDatabaseName("work_orders_created_by_user_id_idx");

            e.HasOne(w => w.Equipment).WithMany()
                .HasForeignKey(w => w.EquipmentId)
                .HasConstraintName("work_orders_equipment_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.CreatedBy).WithMany()
                .HasForeignKey(w => w.CreatedByUserId)
                .HasConstraintName("work_orders_created_by_user_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.Assignee).WithMany()
                .HasForeignKey(w => w.AssigneeUserId)
                .IsRequired(false)
                .HasConstraintName("work_orders_assignee_user_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ['New', 'Closed'] → "'New', 'Closed'" для CHECK … IN (…).
    private static string SqlList(IEnumerable<string> codes) =>
        string.Join(", ", codes.Select(c => $"'{c}'"));
}
