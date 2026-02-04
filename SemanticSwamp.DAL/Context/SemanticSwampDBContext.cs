using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SemanticSwamp.DAL.EFModels;

namespace SemanticSwamp.DAL.Context;

public partial class SemanticSwampDBContext : DbContext
{
    public SemanticSwampDBContext(DbContextOptions<SemanticSwampDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Collection> Collections { get; set; }

    public virtual DbSet<DocumentUpload> DocumentUploads { get; set; }

    public virtual DbSet<DocumentUploadTerm> DocumentUploadTerms { get; set; }

    public virtual DbSet<IdTracker> IdTrackers { get; set; }

    public virtual DbSet<Term> Terms { get; set; }

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
            entity.Property(e => e.Summary).IsUnicode(false);

            entity.HasOne(d => d.Category).WithMany(p => p.DocumentUploads)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentUploads_Categories");

            entity.HasOne(d => d.Collection).WithMany(p => p.DocumentUploads)
                .HasForeignKey(d => d.CollectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentUploads_Collections");
        });

        modelBuilder.Entity<DocumentUploadTerm>(entity =>
        {
            entity.HasOne(d => d.DocumentUpload).WithMany(p => p.DocumentUploadTerms)
                .HasForeignKey(d => d.DocumentUploadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentUploadTerms_DocumentUploads");

            entity.HasOne(d => d.Term).WithMany(p => p.DocumentUploadTerms)
                .HasForeignKey(d => d.TermId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentUploadTerms_Terms");
        });

        modelBuilder.Entity<IdTracker>(entity =>
        {
            entity.ToTable("IdTracker");
        });

        modelBuilder.Entity<Term>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(2000)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
