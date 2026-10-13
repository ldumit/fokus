# Fokus

Sprint analytics dashboard that connects to Jira and surfaces metrics Jira doesn't — per-developer throughput, scope change analysis, carry-over tracking, cycle time breakdowns, and more. No Jira plugin required. Works with any Jira tier, including Free.

## Why

Jira's built-in reports don't answer the questions Scrum Masters and Engineering Managers actually ask: Who delivered what? How much scope changed mid-sprint? Which tickets keep carrying over? Where are the cycle time bottlenecks?

Fokus reads sprint data from the Jira REST API, computes everything locally, and presents it in a single dashboard.

## Features

- **Sprint Health Score** — composite RAG rating with configurable thresholds and per-metric breakdown
- **Scope Change** — disruption rate, change classification, and burnup chart
- **Carry-Over Tracking** — carry-over rate, zombie tickets, destination sprint tracking
- **Cycle Time** — stage durations, percentiles, outlier detection, rework flagging
- **Developer Throughput** — per-developer story-point delivery, feature vs. bug split, rolling averages
- **Bug Ratio Alerts** — bug vs. non-bug SP ratio monitoring
- **Epic Progress** — dual tracking, velocity-based delivery projections
- **Sub-Team Filtering** — tag developers to sub-teams, filter every metric accordingly
- **Manual Sync** — you control when data syncs, per sprint or full backfill

## Tech Stack

- **Backend:** .NET 10 / ASP.NET Core, FastEndpoints, EF Core + SQLite
- **Frontend:** Vue 3, TypeScript, Pinia, Tailwind CSS
- **Auth:** Google OAuth
- **Integration:** Jira REST API (Basic Auth with Atlassian API token)

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js `^20.19.0 || >=22.12.0` (the range the front-end build tools require)
- A Jira account with an [API token](https://id.atlassian.com/manage-profile/security/api-tokens)
- Google OAuth credentials from [Google Cloud Console](https://console.cloud.google.com/) (for authentication)

Running the app needs your own Jira account (the free tier works) and your own Google OAuth credentials. There is no sample data or demo mode: the dashboard shows the sprints it syncs from your Jira project.

### Setup

Every command below runs from the repository root (the folder the clone creates) unless the step says otherwise.

1. Clone the repo (the public URL is set when the repository is published):
   ```
   git clone <public-repo-url> fokus
   cd fokus
   ```

2. Install frontend dependencies:
   ```
   npm ci --prefix client
   ```

3. Create your local settings file `src/Services/Fokus/Fokus.API/appsettings.Development.json` from the committed template (the file is git-ignored, so your secrets stay local):
   ```
   cp src/Services/Fokus/Fokus.API/appsettings.json src/Services/Fokus/Fokus.API/appsettings.Development.json
   ```

4. In `src/Services/Fokus/Fokus.API/appsettings.Development.json`, replace the `<…>` placeholders with your values:
   - `Google:ClientId`, `Google:ClientSecret` — your OAuth 2.0 client (a web application client with `http://localhost:5000/auth/callback` registered as an authorized redirect URI — the backend's address, even when you open the app on 5173, because the dev server forwards `/auth` to the backend on port 5000)
   - `Jira:InstanceUrl` (e.g. `https://your-org.atlassian.net`), `Jira:Email`, `Jira:ApiToken`, `Jira:ProjectKey`

5. Run the backend:
   ```
   dotnet run --project src/Services/Fokus/Fokus.API
   ```

6. Run the frontend (a second terminal, from the repository root):
   ```
   npm run dev --prefix client
   ```

7. Open `http://localhost:5173` in your browser.

## Deployment

See [docs/deployment-azure.md](docs/deployment-azure.md) for Azure App Service deployment instructions.

The deploy script reads a config file that only Azure deployment needs — skip this for local development. From the repository root:
```
mkdir -p .credentials
cp deploy-config.example.ps1 .credentials/deploy-config.ps1
```
Then fill in your values in `.credentials/deploy-config.ps1` and run `deploy.ps1`.

## License

MIT — see [LICENSE](LICENSE) for details.