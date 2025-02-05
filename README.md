# DAVE Benchmarks

This solution contains the benchmarking tools and web interface for the Dynamics of Australian VEgetation (DAVE) model.

## Project Structure

- `Dave.Benchmarks.Web` - ASP.NET Core MVC web application for visualizing benchmark results
- `Dave.Benchmarks.Core` - Shared library containing data models, database access, and business logic
- `Dave.Benchmarks.CLI` - Command-line tool for uploading model outputs to the database
- `Dave.Benchmarks.Tests` - Unit tests project

## Requirements

- .NET 9.0 sdk
- MariaDB/MySQL Server
- Node.js (for client-side libraries)

## Development Setup

1. Install the .NET 9.0 SDK
2. Install and configure MariaDB/MySQL
3. Update the connection string in `appsettings.json`
4. Run the following commands:

```bash
# Restore dependencies
dotnet restore

# Run the web application
cd Dave.Benchmarks.Web
dotnet run
```

## Database Setup

TODO: Add instructions for setting up the database schema and initial data

## CLI Tool Usage

TODO: Add instructions for using the CLI tool to upload model outputs
