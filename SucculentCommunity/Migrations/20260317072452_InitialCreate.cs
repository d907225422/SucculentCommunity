using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SucculentCommunity.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Announcement",
                columns: table => new
                {
                    AnnounceID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcement", x => x.AnnounceID);
                });

            migrationBuilder.CreateTable(
                name: "Member",
                columns: table => new
                {
                    MemberID = table.Column<string>(type: "nchar(5)", maxLength: 5, nullable: false),
                    Account = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nchar(1)", maxLength: 1, nullable: false),
                    RegisterDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Member", x => x.MemberID);
                });

            migrationBuilder.CreateTable(
                name: "Species",
                columns: table => new
                {
                    SpeciesID = table.Column<string>(type: "nchar(5)", maxLength: 5, nullable: false),
                    CommonName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScientificName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Family = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Genus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GrowthSeason = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LightGuide = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WaterGuide = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SoilMix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpeciesImg = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Species", x => x.SpeciesID);
                });

            migrationBuilder.CreateTable(
                name: "Plant",
                columns: table => new
                {
                    PlantID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberID = table.Column<string>(type: "nchar(5)", maxLength: 5, nullable: false),
                    SpeciesID = table.Column<string>(type: "nchar(5)", maxLength: 5, nullable: true),
                    PlantNickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlantImage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plant", x => x.PlantID);
                    table.ForeignKey(
                        name: "FK_Plant_Member_MemberID",
                        column: x => x.MemberID,
                        principalTable: "Member",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Plant_Species_SpeciesID",
                        column: x => x.SpeciesID,
                        principalTable: "Species",
                        principalColumn: "SpeciesID");
                });

            migrationBuilder.CreateTable(
                name: "Post",
                columns: table => new
                {
                    PostID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberID = table.Column<string>(type: "nchar(5)", maxLength: 5, nullable: false),
                    PlantID = table.Column<long>(type: "bigint", nullable: true),
                    PostType = table.Column<string>(type: "nchar(1)", maxLength: 1, nullable: false),
                    PostTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostImage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsPublic = table.Column<string>(type: "nchar(1)", maxLength: 1, nullable: false),
                    IsPinned = table.Column<string>(type: "nchar(1)", maxLength: 1, nullable: false),
                    LikeCount = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Post", x => x.PostID);
                    table.ForeignKey(
                        name: "FK_Post_Member_MemberID",
                        column: x => x.MemberID,
                        principalTable: "Member",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Post_Plant_PlantID",
                        column: x => x.PlantID,
                        principalTable: "Plant",
                        principalColumn: "PlantID");
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    CommentID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostID = table.Column<long>(type: "bigint", nullable: false),
                    MemberID = table.Column<string>(type: "nchar(5)", maxLength: 5, nullable: false),
                    CommentContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommentImage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CommentTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.CommentID);
                    table.ForeignKey(
                        name: "FK_Comment_Member_MemberID",
                        column: x => x.MemberID,
                        principalTable: "Member",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comment_Post_PostID",
                        column: x => x.PostID,
                        principalTable: "Post",
                        principalColumn: "PostID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Favorite",
                columns: table => new
                {
                    FavoriteID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberID = table.Column<string>(type: "nchar(5)", maxLength: 5, nullable: false),
                    FavType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PostID = table.Column<long>(type: "bigint", nullable: true),
                    SpeciesID = table.Column<string>(type: "nchar(5)", maxLength: 5, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorite", x => x.FavoriteID);
                    table.ForeignKey(
                        name: "FK_Favorite_Member_MemberID",
                        column: x => x.MemberID,
                        principalTable: "Member",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Favorite_Post_PostID",
                        column: x => x.PostID,
                        principalTable: "Post",
                        principalColumn: "PostID");
                    table.ForeignKey(
                        name: "FK_Favorite_Species_SpeciesID",
                        column: x => x.SpeciesID,
                        principalTable: "Species",
                        principalColumn: "SpeciesID");
                });

            migrationBuilder.CreateTable(
                name: "PostLike",
                columns: table => new
                {
                    PostLikeID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostID = table.Column<long>(type: "bigint", nullable: false),
                    MemberID = table.Column<string>(type: "nchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostLike", x => x.PostLikeID);
                    table.ForeignKey(
                        name: "FK_PostLike_Member_MemberID",
                        column: x => x.MemberID,
                        principalTable: "Member",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostLike_Post_PostID",
                        column: x => x.PostID,
                        principalTable: "Post",
                        principalColumn: "PostID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comment_MemberID",
                table: "Comment",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_PostID",
                table: "Comment",
                column: "PostID");

            migrationBuilder.CreateIndex(
                name: "IX_Favorite_MemberID",
                table: "Favorite",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_Favorite_PostID",
                table: "Favorite",
                column: "PostID");

            migrationBuilder.CreateIndex(
                name: "IX_Favorite_SpeciesID",
                table: "Favorite",
                column: "SpeciesID");

            migrationBuilder.CreateIndex(
                name: "IX_Plant_MemberID",
                table: "Plant",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_Plant_SpeciesID",
                table: "Plant",
                column: "SpeciesID");

            migrationBuilder.CreateIndex(
                name: "IX_Post_MemberID",
                table: "Post",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_Post_PlantID",
                table: "Post",
                column: "PlantID");

            migrationBuilder.CreateIndex(
                name: "IX_PostLike_MemberID",
                table: "PostLike",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_PostLike_PostID",
                table: "PostLike",
                column: "PostID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcement");

            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropTable(
                name: "Favorite");

            migrationBuilder.DropTable(
                name: "PostLike");

            migrationBuilder.DropTable(
                name: "Post");

            migrationBuilder.DropTable(
                name: "Plant");

            migrationBuilder.DropTable(
                name: "Member");

            migrationBuilder.DropTable(
                name: "Species");
        }
    }
}
