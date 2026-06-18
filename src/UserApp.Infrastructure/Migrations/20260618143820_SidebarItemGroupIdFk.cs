using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SidebarItemGroupIdFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Conditionally drop GroupName if it still exists
            migrationBuilder.Sql(@"
                SET @stmt = IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SidebarItems' AND COLUMN_NAME = 'GroupName') > 0,
                    'ALTER TABLE SidebarItems DROP COLUMN GroupName',
                    'SELECT 1'
                );
                PREPARE s FROM @stmt;
                EXECUTE s;
                DEALLOCATE PREPARE s;
            ");

            // Conditionally add GroupId if it doesn't exist yet
            migrationBuilder.Sql(@"
                SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SidebarItems' AND COLUMN_NAME = 'GroupId');
                SET @stmt = IF(@col_exists = 0,
                    CONCAT('ALTER TABLE SidebarItems ADD COLUMN GroupId char(36) COLLATE ascii_general_ci NOT NULL DEFAULT ', CHAR(39), '00000000-0000-0000-0000-000000000000', CHAR(39)),
                    'SELECT 1'
                );
                PREPARE s FROM @stmt;
                EXECUTE s;
                DEALLOCATE PREPARE s;
            ");

            // Conditionally create index if it doesn't exist
            migrationBuilder.Sql(@"
                SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SidebarItems' AND INDEX_NAME = 'IX_SidebarItems_GroupId');
                SET @stmt = IF(@idx_exists = 0,
                    'CREATE INDEX IX_SidebarItems_GroupId ON SidebarItems (GroupId)',
                    'SELECT 1'
                );
                PREPARE s FROM @stmt;
                EXECUTE s;
                DEALLOCATE PREPARE s;
            ");

            // Fix orphaned rows: assign them to the first available SidebarGroup
            migrationBuilder.Sql(@"
                SET @first_group = (SELECT Id FROM SidebarGroups ORDER BY DisplayOrder LIMIT 1);
                UPDATE SidebarItems SET GroupId = @first_group
                WHERE @first_group IS NOT NULL
                  AND GroupId NOT IN (SELECT Id FROM SidebarGroups WHERE Id IS NOT NULL);
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Conditionally drop FK if it exists
            migrationBuilder.Sql(@"
                SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SidebarItems' AND CONSTRAINT_NAME = 'FK_SidebarItems_SidebarGroups_GroupId');
                SET @stmt = IF(@fk_exists > 0,
                    'ALTER TABLE SidebarItems DROP FOREIGN KEY FK_SidebarItems_SidebarGroups_GroupId',
                    'SELECT 1'
                );
                PREPARE s FROM @stmt;
                EXECUTE s;
                DEALLOCATE PREPARE s;
            ");

            // Conditionally drop index if it exists
            migrationBuilder.Sql(@"
                SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SidebarItems' AND INDEX_NAME = 'IX_SidebarItems_GroupId');
                SET @stmt = IF(@idx_exists > 0,
                    'DROP INDEX IX_SidebarItems_GroupId ON SidebarItems',
                    'SELECT 1'
                );
                PREPARE s FROM @stmt;
                EXECUTE s;
                DEALLOCATE PREPARE s;
            ");

            // Conditionally drop GroupId column if it exists
            migrationBuilder.Sql(@"
                SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SidebarItems' AND COLUMN_NAME = 'GroupId');
                SET @stmt = IF(@col_exists > 0,
                    'ALTER TABLE SidebarItems DROP COLUMN GroupId',
                    'SELECT 1'
                );
                PREPARE s FROM @stmt;
                EXECUTE s;
                DEALLOCATE PREPARE s;
            ");

            // Conditionally add GroupName back if it doesn't exist
            migrationBuilder.Sql(@"
                SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SidebarItems' AND COLUMN_NAME = 'GroupName');
                SET @stmt = IF(@col_exists = 0,
                    'ALTER TABLE SidebarItems ADD COLUMN GroupName varchar(100) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''''',
                    'SELECT 1'
                );
                PREPARE s FROM @stmt;
                EXECUTE s;
                DEALLOCATE PREPARE s;
            ");
        }
    }
}
