﻿// <auto-generated />
using HousePredictionAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace HousePredictionAPI.Migrations
{
    [DbContext(typeof(HousePredictionDBContext))]
    [Migration("20250606164927_saleprice")]
    partial class saleprice
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("HousePredictionAPI.Entities.HouseDetails", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("FullBath")
                        .HasColumnType("int");

                    b.Property<int>("GarageArea")
                        .HasColumnType("int");

                    b.Property<int>("GrLivArea")
                        .HasColumnType("int");

                    b.Property<string>("Neighborhood")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("OverallQual")
                        .HasColumnType("int");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("TotRmsAbvGrd")
                        .HasColumnType("int");

                    b.Property<int>("TotalBsmtSF")
                        .HasColumnType("int");

                    b.Property<int>("YearBuilt")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("HouseDetails");
                });
#pragma warning restore 612, 618
        }
    }
}
