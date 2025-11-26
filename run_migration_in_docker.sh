#!/bin/bash
# Script to run the SQL migration inside Docker
# Usage: ./run_migration_in_docker.sh

echo "Running Player Names Migration in Docker..."

# Option 1: If you have the SQL Server container name/id
# Replace 'your-sql-container' with your actual SQL Server container name
SQL_CONTAINER="your-sql-container"

# Option 2: If you're connecting to SQL Server from outside Docker
# You can use sqlcmd directly if it's installed on your host
# Or use docker exec to run sqlcmd inside the SQL Server container

# Method 1: Using docker exec (if SQL Server tools are in the container)
# docker exec -i $SQL_CONTAINER /opt/mssql-tools/bin/sqlcmd \
#     -S localhost -U sa -P 'YourPassword' \
#     -d YourDatabase \
#     -i update_player_names.sql

# Method 2: Using sqlcmd from host (if installed)
# sqlcmd -S your-sql-server:1433 -U sa -P 'YourPassword' \
#     -d YourDatabase \
#     -i update_player_names.sql

# Method 3: Copy file into container and execute
# docker cp update_player_names.sql $SQL_CONTAINER:/tmp/
# docker exec $SQL_CONTAINER /opt/mssql-tools/bin/sqlcmd \
#     -S localhost -U sa -P 'YourPassword' \
#     -d YourDatabase \
#     -i /tmp/update_player_names.sql

echo "Please update this script with your actual SQL Server connection details."
echo "Then uncomment and run the appropriate method above."
