# DAVE Benchmarks

This solution contains the benchmarking tools and web interface for the Dynamics of Australian VEgetation (DAVE) model.

## Project Structure

- `Dave.Benchmarks.Web` - ASP.NET Core MVC web application for visualizing benchmark results
- `Dave.Benchmarks.Core` - Shared library containing data models, database access, and business logic
- `Dave.Benchmarks.CLI` - Command-line tool for uploading model outputs to the database
- `Dave.Benchmarks.Tests` - Unit tests project

## Requirements

- .NET 9.0 SDK and ASP.NET Core Runtime
- MariaDB/MySQL Server
- Node.js (for client-side libraries)

## Development Setup

1. Install the .NET 9.0 SDK and ASP.NET Core Runtime
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

Configure GitLab CI and curator authentication in `appsettings.json`, or
preferably with environment variables in deployed environments:

```json
{
  "Authentication": {
    "Schemes": {
      "Bearer": {
        "Authority": "https://gitlab.example.com",
        "MapInboundClaims": false,
        "ValidAudiences": [ "https://benchmarks.example.com" ]
      }
    }
  },
  "Authorisation": {
    "AllowedGitlabProjectIds": [ "12345" ]
  },
  "GitLabOAuth": {
    "TokenIssuer": "https://benchmarks.example.com",
    "SigningKey": "<base64-encoded random key of at least 32 bytes>",
    "TokenLifetimeMinutes": 60
  }
}
```

`Authority` is the base URI of the one trusted GitLab instance.
`ValidAudiences` contains the audience configured for job ID tokens. The server
also uses the authority's API to verify curator access. `TokenIssuer` is the
public HTTPS URI identifying this benchmark server, and `SigningKey` signs its
short-lived curator tokens. Keep the signing key out of source control; for
example, configure these values as:

```text
Authentication__Schemes__Bearer__Authority
Authentication__Schemes__Bearer__ValidAudiences__0
Authorisation__AllowedGitlabProjectIds__0
GitLabOAuth__TokenIssuer
GitLabOAuth__SigningKey
```

Project IDs are used instead of project paths so renaming a project does not
change the trust rule. Production authority, audience, and token-issuer URIs
must all use HTTPS.

The prediction, submission, and evaluation mutation APIs require a valid ID
token from an allowed GitLab project. Baseline acceptance and destructive data
operations additionally require the token's `ref_protected` claim to be
`true`. Observation ingestion instead requires a curator token issued by the
benchmark server after it verifies that the interactive GitLab user has at
least Maintainer access to one configured project.

Request a short-lived ID token in each GitLab job that calls the API. Its `aud`
must exactly match one configured `ValidAudiences` value:

```yaml
benchmark_sites:
  id_tokens:
    BENCHMARKS_ID_TOKEN:
      aud: https://benchmarks.example.com
  script:
    - export DAVE_BENCHMARKS_TOKEN="$BENCHMARKS_ID_TOKEN"
    - >-
      dotnet run --project src/Dave.Benchmarks.CLI -- benchmark
      --repo-path .
      --name "MR site benchmarks"
      --description "Site benchmarks for ${CI_COMMIT_SHA}"
      --climate-dataset OzFlux
      --temporal-resolution 3-hourly
      --merge-request-id "$CI_MERGE_REQUEST_IID"
      --pipeline-id "$CI_PIPELINE_ID"
      --source-branch "$CI_MERGE_REQUEST_SOURCE_BRANCH_NAME"
      --target-branch "$CI_MERGE_REQUEST_TARGET_BRANCH_NAME"
      --commit-sha "$CI_COMMIT_SHA"
      --commit-message "$CI_COMMIT_MESSAGE"
```

The importer reads `DAVE_BENCHMARKS_TOKEN` and sends it as an HTTP bearer token
for all production API requests. `benchmark` creates a submission, directly
invokes the site importer, completes the submission, starts one aggregate
evaluation, and polls it. It exits 0 on pass, 2 on a completed gate failure,
and 1 on an operational/import/evaluation error. The default timeout is 1800
seconds and the default polling interval is 5 seconds; override them with
`--timeout-seconds` and `--poll-interval-seconds`.

The lower-level `site`, `gridded`, and `evaluate --submission-id ...` verbs
remain available for development and debugging. Partial imports are retained
by default. Pass `--cleanup-on-failure` to `site`, `gridded`, or `benchmark` to
restore the old behavior of deleting the partially imported dataset group.

For a manual request with curl:

```bash
curl \
  --header "Authorization: Bearer ${BENCHMARKS_ID_TOKEN}" \
  --header "Content-Type: application/json" \
  --data '{"benchmarkSubmissionId":42}' \
  https://benchmarks.example.com/api/evaluation/run
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

### Importing observations

Register a GitLab OAuth application for the importer and enable the device
authorization flow. Configure its application/client ID as
`GitLabOAuthClientId` in the CLI settings; no client secret is embedded in the
CLI. Running the observation importer starts the GitLab device-login flow,
exchanges the resulting OAuth access token with the benchmark server, and uses
the returned short-lived curator token for ingestion.

An observation release is either `site` or `gridded`; it cannot mix the two.
Site files are split into one dataset per distinct site name and contain no
invented coordinates. A release becomes immutable when completed. With
`--activate`, activation occurs after completion and atomically replaces the
active version of the same source/collection.

Example site manifest:

```yaml
collection: ozflux
source: ozflux
version: 2026-08-17
description: OzFlux tower observations
kind: site
metadata: '{}'
files:
  - path: flux.csv.gz
    date_column: date
    site_column: site
    temporal_resolution: daily
    variables:
      - column: gpp
        units: gC/m2/day
        target:
          output: file_dave_dgpp
          layer: total
```

Import and activate it with:

```bash
dotnet run --project src/Dave.Benchmarks.CLI -- \
  observations --manifest observations.yaml --activate
```

Partial observation imports are retained by default for diagnosis. During
development of a new source, add `--cleanup-on-failure` to delete the release
and all datasets created for it if any import or activation step fails. An
active observation release is never deleted automatically.

Input may be plain CSV or gzip-compressed CSV (`.gz`). Gridded manifests use
`kind: gridded`, declare `longitude_column` and `latitude_column`, and select
`matching_strategy: exact` or `nearest` (with `max_distance_km` for nearest).
The initial importer supports gridcell-level variables and requires all files
in one release to use the same temporal resolution. A `target` maps an arbitrary
source column to a model output definition and layer. The importer derives the
canonical variable name, description, aggregation level, units, and layer from
that definition and rejects incompatible declared units or temporal resolution.

### Browsing results

The web navigation separates **Predictions** and **Observations** while using
the same dataset, variable, and tabular-data explorer. Observation pages are
read-only in normal deployments. Dataset/release delete controls are rendered
only in Development; their endpoints still require a protected GitLab token in
non-development environments.

The **Evaluations** page groups benchmark submissions and runs by GitLab merge
request. Selecting an MR shows its tested commits, pipelines, and evaluation
attempts. A run detail page shows its aggregate outcome, per-dataset outcome,
baseline, observation comparisons, and stored metrics.

The **Timeseries** page can overlay multiple dataset/variable selections and
multiple layers from each selection. When its first dataset is site-level,
an enabled-by-default option restricts additional datasets to the same
`SimulationId`; disabling it permits cross-site comparisons. All traces are
restricted to timestamps shared by every selected trace. The
**Relationships** page provides a site-level X-versus-Y view with one arbitrary
X series and one or more Y series, joining every X/Y pair by timestamp.
