using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations;

public partial class AddIdleLockFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IdleLockEnabled",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "IdleLockTimeoutMinutes",
            table: "Users",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "IdleLockLastSeenAt",
            table: "Users",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IdleLockIsLocked",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "IdleLockLockedAt",
            table: "Users",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IdleLockPinHash",
            table: "Users",
            type: "nvarchar(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "IdleLockPinSetAt",
            table: "Users",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "IdleLockPinFailedCount",
            table: "Users",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "IdleLockPinLockoutUntil",
            table: "Users",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IdleLockEnabled", table: "Users");
        migrationBuilder.DropColumn(name: "IdleLockTimeoutMinutes", table: "Users");
        migrationBuilder.DropColumn(name: "IdleLockLastSeenAt", table: "Users");
        migrationBuilder.DropColumn(name: "IdleLockIsLocked", table: "Users");
        migrationBuilder.DropColumn(name: "IdleLockLockedAt", table: "Users");
        migrationBuilder.DropColumn(name: "IdleLockPinHash", table: "Users");
        migrationBuilder.DropColumn(name: "IdleLockPinSetAt", table: "Users");
        migrationBuilder.DropColumn(name: "IdleLockPinFailedCount", table: "Users");
        migrationBuilder.DropColumn(name: "IdleLockPinLockoutUntil", table: "Users");
    }
}
