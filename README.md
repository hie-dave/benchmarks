# DAVE Benchmarks

This solution contains the benchmarking tools and web interface for the Dynamics of Australian VEgetation (DAVE) model.

## Project Structure

- `Dave.Benchmarks.Web` - ASP.NET Core MVC web application for visualizing benchmark results
- `Dave.Benchmarks.Core` - Shared library containing data models, database access, and business logic
- `Dave.Benchmarks.CLI` - Command-line tool for uploading model outputs to the database
- `Dave.Benchmarks.Tests` - Unit tests project

## Requirements

- .NET 8.0 SDK and ASP.NET Core Runtime
- MariaDB/MySQL Server
- Node.js (for client-side libraries)

## Development Setup

1. Install the .NET 8.0 SDK and ASP.NET Core Runtime
2. Install and configure MariaDB:

Follow distribution-specific instructions for installing MariaDB.

Once installed, run these commands:

```bash
sudo mysql_secure_installation
# Follow the prompts to:
# 1. Set root password
# 2. Remove anonymous users
# 3. Disallow root login remotely
# 4. Remove test database
# 5. Reload privilege tables

# Create database and user
sudo mysql -u root -p
```

Then in the MySQL prompt, create the database and user:

```sql
CREATE DATABASE dave_benchmarks;
CREATE USER 'dave'@'localhost' IDENTIFIED BY 'your_password_here';
GRANT ALL PRIVILEGES ON dave_benchmarks.* TO 'dave'@'localhost';
FLUSH PRIVILEGES;
EXIT;
```

3. Update the connection string in `appsettings.json`:

For development environments using Unix sockets:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=/var/run/mysqld/mysqld.sock;database=dave_benchmarks;user=dave"
  }
}
```

For production environments using TCP/IP:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=dave_benchmarks;user=dave;password=your_password_here"
  }
}
```

4. Run the application:

```bash
# Restore dependencies
dotnet restore

# Run the web application
cd Dave.Benchmarks.Web
dotnet run
```

5. Test the database connection:

```bash
curl http://localhost:5069/api/diagnostics/db
```

## Database Management

### Starting/Stopping MariaDB

```bash
# Start MariaDB
sudo systemctl start mariadb

# Stop MariaDB
sudo systemctl stop mariadb

# Check status
sudo systemctl status mariadb

# Enable MariaDB to start on boot
sudo systemctl enable mariadb
```

### Backup and Restore

```bash
# Backup database
mysqldump -u dave -p dave_benchmarks > backup.sql

# Restore database
mysql -u dave -p dave_benchmarks < backup.sql
```

## CLI Tool Usage

TODO: Add instructions for using the CLI tool to upload model outputs
