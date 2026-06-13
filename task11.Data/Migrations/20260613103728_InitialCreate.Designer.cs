using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using task11.Data;

#nullable disable

namespace task11.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260613103728_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("task11.Data.Entities.FinancialOperationEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("Amount")
                        .HasPrecision(18, 2)
                        .HasColumnType("numeric(18,2)");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("DeletedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<string>("Note")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<DateTime>("OccurredAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("OperationTypeId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("UpdatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("WalletId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("OperationTypeId");

                    b.HasIndex("WalletId", "OccurredAtUtc");

                    b.ToTable("financial_operations", (string)null);
                });

            modelBuilder.Entity("task11.Data.Entities.OperationTypeEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("DeletedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<int>("Kind")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<DateTime?>("UpdatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("WalletId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("WalletId", "Name")
                        .IsUnique()
                        .HasFilter("\"IsDeleted\" = false");

                    b.ToTable("operation_types", (string)null);
                });

            modelBuilder.Entity("task11.Data.Entities.UserEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("DeletedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasDefaultValue("User");

                    b.Property<DateTime?>("UpdatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.HasKey("Id");

                    b.HasIndex("Username")
                        .IsUnique()
                        .HasFilter("\"IsDeleted\" = false");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("task11.Data.Entities.WalletEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("BaseCurrency")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(3)
                        .HasColumnType("char(3)")
                        .HasDefaultValue("UAH")
                        .IsFixedLength();

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("DeletedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<Guid?>("OwnerUserId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("UpdatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("OwnerUserId");

                    b.ToTable("wallets", (string)null);
                });

            modelBuilder.Entity("task11.Data.Entities.FinancialOperationEntity", b =>
                {
                    b.HasOne("task11.Data.Entities.OperationTypeEntity", "OperationType")
                        .WithMany("Operations")
                        .HasForeignKey("OperationTypeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("task11.Data.Entities.WalletEntity", "Wallet")
                        .WithMany("Operations")
                        .HasForeignKey("WalletId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("OperationType");

                    b.Navigation("Wallet");
                });

            modelBuilder.Entity("task11.Data.Entities.OperationTypeEntity", b =>
                {
                    b.HasOne("task11.Data.Entities.WalletEntity", "Wallet")
                        .WithMany("OperationTypes")
                        .HasForeignKey("WalletId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Wallet");
                });

            modelBuilder.Entity("task11.Data.Entities.WalletEntity", b =>
                {
                    b.HasOne("task11.Data.Entities.UserEntity", "Owner")
                        .WithMany("OwnedWallets")
                        .HasForeignKey("OwnerUserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("task11.Data.Entities.OperationTypeEntity", b =>
                {
                    b.Navigation("Operations");
                });

            modelBuilder.Entity("task11.Data.Entities.UserEntity", b =>
                {
                    b.Navigation("OwnedWallets");
                });

            modelBuilder.Entity("task11.Data.Entities.WalletEntity", b =>
                {
                    b.Navigation("OperationTypes");

                    b.Navigation("Operations");
                });
#pragma warning restore 612, 618
        }
    }
}
