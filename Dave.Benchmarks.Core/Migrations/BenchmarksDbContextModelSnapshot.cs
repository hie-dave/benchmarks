﻿// <auto-generated />
using System;
using Dave.Benchmarks.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Dave.Benchmarks.Core.Migrations
{
    [DbContext(typeof(BenchmarksDbContext))]
    partial class BenchmarksDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.Dataset", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("SpatialResolution")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("TemporalResolution")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("Datasets");

                    b.UseTptMappingStrategy();
                });

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.Datum", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<int>("DatasetId")
                        .HasColumnType("int");

                    b.Property<double>("Latitude")
                        .HasColumnType("double");

                    b.Property<double>("Longitude")
                        .HasColumnType("double");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.Property<double>("Value")
                        .HasColumnType("double");

                    b.Property<int>("VariableId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("VariableId");

                    b.HasIndex("DatasetId", "VariableId", "Timestamp");

                    b.HasIndex("DatasetId", "VariableId", "Longitude", "Latitude");

                    b.ToTable("Data", (string)null);
                });

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.Variable", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("DatasetId")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Units")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("DatasetId");

                    b.ToTable("Variables", (string)null);
                });

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.ModelPredictionDataset", b =>
                {
                    b.HasBaseType("Dave.Benchmarks.Core.Models.Entities.Dataset");

                    b.Property<string>("ClimateDataset")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<byte[]>("CodePatches")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<byte[]>("CompressedParameters")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<string>("InputDataSource")
                        .HasColumnType("longtext");

                    b.Property<string>("ModelVersion")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.ToTable("Predictions", (string)null);
                });

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.ObservationDataset", b =>
                {
                    b.HasBaseType("Dave.Benchmarks.Core.Models.Entities.Dataset");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.ToTable("Observations", (string)null);
                });

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.Datum", b =>
                {
                    b.HasOne("Dave.Benchmarks.Core.Models.Entities.Dataset", "Dataset")
                        .WithMany("Data")
                        .HasForeignKey("DatasetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Dave.Benchmarks.Core.Models.Entities.Variable", "Variable")
                        .WithMany("Data")
                        .HasForeignKey("VariableId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dataset");

                    b.Navigation("Variable");
                });

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.Variable", b =>
                {
                    b.HasOne("Dave.Benchmarks.Core.Models.Entities.Dataset", "Dataset")
                        .WithMany("Variables")
                        .HasForeignKey("DatasetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dataset");
                });

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.ModelPredictionDataset", b =>
                {
                    b.HasOne("Dave.Benchmarks.Core.Models.Entities.Dataset", null)
                        .WithOne()
                        .HasForeignKey("Dave.Benchmarks.Core.Models.Entities.ModelPredictionDataset", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.ObservationDataset", b =>
                {
                    b.HasOne("Dave.Benchmarks.Core.Models.Entities.Dataset", null)
                        .WithOne()
                        .HasForeignKey("Dave.Benchmarks.Core.Models.Entities.ObservationDataset", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.Dataset", b =>
                {
                    b.Navigation("Data");

                    b.Navigation("Variables");
                });

            modelBuilder.Entity("Dave.Benchmarks.Core.Models.Entities.Variable", b =>
                {
                    b.Navigation("Data");
                });
#pragma warning restore 612, 618
        }
    }
}
