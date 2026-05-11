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
- Node.js 18+
- A Jira account with an [API token](https://id.atlassian.com/manage-profile/security/api-tokens)
- Google OAuth credentials from [Google Cloud Console](https://console.cloud.google.com/) (for authentication)

### Setup

1. Clone the repo:
   ```
   git clone https://github.com/ldumit/fokus.git
   cd fokus
   ```

2. Install frontend dependencies:
   ```
   cd client
   npm install
   ```

3. Configure credentials — copy the example and fill in your values:
   ```
   cp deploy-config.example.ps1 .credentials/deploy-config.ps1
   ```

4. Add your Google OAuth and Jira credentials to `appsettings.Development.json`:
   ```json
   {
     "Google": {
       "ClientId": "<your-client-id>",
       "ClientSecret": "<your-client-secret>"
     },
     "Jira": {
       "InstanceUrl": "https://your-org.atlassian.net",
       "Email": "your-email@example.com",
       "ApiToken": "<your-api-token>"
     }
   }
   ```

5. Run the backend:
   ```
   dotnet run --project src/Services/Fokus/Fokus.API
   ```

6. Run the frontend (separate terminal):
   ```
   cd client
   npm run dev
   ```

7. Open `http://localhost:5173` in your browser.

## Deployment

See [docs/deployment-azure.md](docs/deployment-azure.md) for Azure App Service deployment instructions.

## License

MIT — see [LICENSE](LICENSE) for details.