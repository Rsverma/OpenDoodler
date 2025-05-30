﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenBoardAnim.Library;

#nullable disable

namespace OpenBoardAnim.Library.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.6");

            modelBuilder.Entity("OpenBoardAnim.Library.GraphicEntity", b =>
                {
                    b.Property<int>("GraphicID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SVGText")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("GraphicID");

                    b.ToTable("Graphics");
                });

            modelBuilder.Entity("OpenBoardAnim.Library.ProjectEntity", b =>
                {
                    b.Property<int>("ProjectID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LatestLaunchTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("SceneCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("ProjectID");

                    b.ToTable("Projects");
                });
#pragma warning restore 612, 618
        }
    }
}
