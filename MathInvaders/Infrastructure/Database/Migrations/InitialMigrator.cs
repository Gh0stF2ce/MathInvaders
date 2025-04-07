using FluentMigrator;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Generic;

namespace MathInvaders.Infrastructure.Database.Migrations
{
    [Migration(202506041251)]
    public class InitialMigrator : Migration
    {
        public override void Up()
        {
            Create.Table("users")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("username").AsString(50).NotNullable().Unique()
                .WithColumn("password").AsString(100).NotNullable();

            // Тестовые данные
            Insert.IntoTable("users")
                .Row(new { username = "player1", password = "pass1" })
                .Row(new { username = "player2", password = "pass2" });
        }

        public override void Down()
        {
            Delete.Table("users");
        }
    }
}

