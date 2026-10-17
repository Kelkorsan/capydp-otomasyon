using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapYdp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AkademikDonemler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonemAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Yil = table.Column<int>(type: "int", nullable: false),
                    DonemTipi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AkademikDonemler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dersler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DersKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DersAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Akts = table.Column<int>(type: "int", nullable: false),
                    Yariyil = table.Column<int>(type: "int", nullable: false),
                    DersTuru = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dersler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IslemKayitlari",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: true),
                    IslemTuru = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TabloAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KayitId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EskiDeger = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YeniDeger = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAdresi = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IslemKayitlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ogrenciler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    OgrenciNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProgramTuru = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnadalFakulte = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AnadalBolum = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    KabulDonemiId = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AnadalGano = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    GanoKaynagi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GanoGuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ogrenciler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ogrenciler_AkademikDonemler_KabulDonemiId",
                        column: x => x.KabulDonemiId,
                        principalTable: "AkademikDonemler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ogrenciler_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MuafiyetKurallari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CapDersId = table.Column<int>(type: "int", nullable: false),
                    AnadalDersKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnadalDersAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuafiyetKurallari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuafiyetKurallari_Dersler_CapDersId",
                        column: x => x.CapDersId,
                        principalTable: "Dersler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DersKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciId = table.Column<int>(type: "int", nullable: false),
                    DersId = table.Column<int>(type: "int", nullable: false),
                    DonemId = table.Column<int>(type: "int", nullable: false),
                    KayitOnayDurumu = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    KayitRedGerekce = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    KayitOnayTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KayitOnaylayanId = table.Column<int>(type: "int", nullable: true),
                    HarfNotu = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    PuanNotu = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    NotOnayDurumu = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NotRedGerekce = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NotOnayTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotOnaylayanId = table.Column<int>(type: "int", nullable: true),
                    DersDurumu = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DenemeSayisi = table.Column<int>(type: "int", nullable: false),
                    GirenId = table.Column<int>(type: "int", nullable: false),
                    GirisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeriyeDonukMu = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DersKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DersKayitlari_AkademikDonemler_DonemId",
                        column: x => x.DonemId,
                        principalTable: "AkademikDonemler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DersKayitlari_AspNetUsers_GirenId",
                        column: x => x.GirenId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DersKayitlari_AspNetUsers_KayitOnaylayanId",
                        column: x => x.KayitOnaylayanId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DersKayitlari_AspNetUsers_NotOnaylayanId",
                        column: x => x.NotOnaylayanId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DersKayitlari_Dersler_DersId",
                        column: x => x.DersId,
                        principalTable: "Dersler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DersKayitlari_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OgrenciDersListeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciId = table.Column<int>(type: "int", nullable: false),
                    DersId = table.Column<int>(type: "int", nullable: false),
                    OzelSartliMi = table.Column<bool>(type: "bit", nullable: false),
                    Durum = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DegisiklikGerekce = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EklemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DegisiklikTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgrenciDersListeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OgrenciDersListeleri_Dersler_DersId",
                        column: x => x.DersId,
                        principalTable: "Dersler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OgrenciDersListeleri_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AkademikDonemler_Yil_DonemTipi",
                table: "AkademikDonemler",
                columns: new[] { "Yil", "DonemTipi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DersKayitlari_DersId",
                table: "DersKayitlari",
                column: "DersId");

            migrationBuilder.CreateIndex(
                name: "IX_DersKayitlari_DonemId",
                table: "DersKayitlari",
                column: "DonemId");

            migrationBuilder.CreateIndex(
                name: "IX_DersKayitlari_GirenId",
                table: "DersKayitlari",
                column: "GirenId");

            migrationBuilder.CreateIndex(
                name: "IX_DersKayitlari_KayitOnaylayanId",
                table: "DersKayitlari",
                column: "KayitOnaylayanId");

            migrationBuilder.CreateIndex(
                name: "IX_DersKayitlari_NotOnaylayanId",
                table: "DersKayitlari",
                column: "NotOnaylayanId");

            migrationBuilder.CreateIndex(
                name: "IX_DersKayitlari_OgrenciId",
                table: "DersKayitlari",
                column: "OgrenciId");

            migrationBuilder.CreateIndex(
                name: "IX_Dersler_DersKodu",
                table: "Dersler",
                column: "DersKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IslemKayitlari_TabloAdi_KayitId",
                table: "IslemKayitlari",
                columns: new[] { "TabloAdi", "KayitId" });

            migrationBuilder.CreateIndex(
                name: "IX_IslemKayitlari_Tarih",
                table: "IslemKayitlari",
                column: "Tarih");

            migrationBuilder.CreateIndex(
                name: "IX_MuafiyetKurallari_CapDersId",
                table: "MuafiyetKurallari",
                column: "CapDersId");

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciDersListeleri_DersId",
                table: "OgrenciDersListeleri",
                column: "DersId");

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciDersListeleri_OgrenciId_DersId",
                table: "OgrenciDersListeleri",
                columns: new[] { "OgrenciId", "DersId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ogrenciler_KabulDonemiId",
                table: "Ogrenciler",
                column: "KabulDonemiId");

            migrationBuilder.CreateIndex(
                name: "IX_Ogrenciler_KullaniciId",
                table: "Ogrenciler",
                column: "KullaniciId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ogrenciler_OgrenciNo",
                table: "Ogrenciler",
                column: "OgrenciNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DersKayitlari");

            migrationBuilder.DropTable(
                name: "IslemKayitlari");

            migrationBuilder.DropTable(
                name: "MuafiyetKurallari");

            migrationBuilder.DropTable(
                name: "OgrenciDersListeleri");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Dersler");

            migrationBuilder.DropTable(
                name: "Ogrenciler");

            migrationBuilder.DropTable(
                name: "AkademikDonemler");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
