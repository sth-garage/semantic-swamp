using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SemanticSwamp.DAL.EFModels;

namespace SemanticSwamp.DAL.Context;

public partial class SemanticSwampDBContext : DbContext
{
    public SemanticSwampDBContext()
    {
    }

    public SemanticSwampDBContext(DbContextOptions<SemanticSwampDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Collection> Collections { get; set; }

    public virtual DbSet<DocumentUpload> DocumentUploads { get; set; }

    public virtual DbSet<DocumentUploadTerm> DocumentUploadTerms { get; set; }

    public virtual DbSet<Term> Terms { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=127.0.0.1;Initial Catalog=SemanticSwamp;User Id=semanticSwampServiceLogin;Password=Testing777!!;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(2000)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(2000)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DocumentUpload>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Uploads");

            entity.Property(e => e.Base64Data).IsUnicode(false);
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("(getdate())", "DF_Uploads_CreatedOn")
                .HasColumnType("datetime");
            entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_DocumentUploads_IsActive");

            entity.HasOne(d => d.Category).WithMany(p => p.DocumentUploads)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Categories_DocumentUploads");

            entity.HasOne(d => d.Collection).WithMany(p => p.DocumentUploads)
                .HasForeignKey(d => d.CollectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Collections_DocumentUploads");
        });

        modelBuilder.Entity<DocumentUploadTerm>(entity =>
        {
            entity.HasOne(d => d.Term).WithMany(p => p.DocumentUploadTerms)
                .HasForeignKey(d => d.TermId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentUploadTerms_Terms");
        });

        modelBuilder.Entity<Term>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name)
                .HasMaxLength(2000)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
