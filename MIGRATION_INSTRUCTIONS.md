# Migration Instructions: Make Player FirstName and LastName Nullable

Since you're running both your ASP.NET app and SQL database in Docker, you can't use `dotnet ef migrations` directly. Here are several ways to run the migration:

## Option 1: Run SQL Script Directly in SQL Server Container

### Step 1: Copy the SQL script into your SQL Server container
```bash
docker cp update_player_names.sql <your-sql-container-name>:/tmp/
```

### Step 2: Execute the script inside the container
```bash
docker exec -i <your-sql-container-name> /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P '<your-password>' \
    -d <your-database-name> \
    -i /tmp/update_player_names.sql
```

## Option 2: Connect from Host Machine

If you have `sqlcmd` installed on your host machine or can access SQL Server from outside Docker:

```bash
sqlcmd -S <your-sql-server-host>:<port> -U sa -P '<your-password>' \
    -d <your-database-name> \
    -i update_player_names.sql
```

## Option 3: Use Azure Data Studio / SQL Server Management Studio

1. Connect to your SQL Server instance (running in Docker)
2. Open the `update_player_names.sql` file
3. Execute it

## Option 4: Use Docker Compose (if applicable)

If you're using docker-compose, you can add a one-time migration service:

```yaml
migration:
  image: mcr.microsoft.com/mssql-tools
  volumes:
    - ./update_player_names.sql:/tmp/migration.sql
  command: >
    /opt/mssql-tools/bin/sqlcmd
    -S <sql-server-service-name>
    -U sa
    -P '<your-password>'
    -d <your-database-name>
    -i /tmp/migration.sql
  depends_on:
    - <your-sql-service>
```

## What the Script Does

1. **Updates Data**: Sets `FirstName = NULL` where `FirstName = 'Random'` and `LastName = NULL` where `LastName = 'Randomov'`
2. **Alters Schema**: Changes `FirstName` and `LastName` columns from `NOT NULL` to `NULL` (preserving NVARCHAR(30) type)
3. **Verifies**: Shows the column definitions to confirm the changes

## Important Notes

- **Backup First**: Always backup your database before running migrations
- **Test in Dev**: Test the script on a development/staging database first
- **Check Constraints**: If you get errors about constraints, you may need to drop them first:
  ```sql
  -- Find constraints
  SELECT name, type_desc 
  FROM sys.objects 
  WHERE parent_object_id = OBJECT_ID('Players') 
    AND type_desc LIKE '%CONSTRAINT%';
  
  -- Drop if needed (replace <constraint-name>)
  ALTER TABLE Players DROP CONSTRAINT <constraint-name>;
  ```

## Verification

After running the migration, verify it worked:

```sql
SELECT 
    COLUMN_NAME,
    IS_NULLABLE,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Players' 
    AND COLUMN_NAME IN ('FirstName', 'LastName');
```

Both columns should show `IS_NULLABLE = 'YES'`.

