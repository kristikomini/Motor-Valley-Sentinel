# Deploying to Azure

The local stack runs on `docker compose`; the same five services deploy to Azure
by swapping each self-hosted container for a managed PaaS equivalent and running the
application services as **Azure Container Apps**. The application code does not change —
the broker, database, cache, and realtime transport are all reached through the same
interfaces, driven entirely by connection strings.

## Local → Azure mapping

| Local (docker-compose) | Azure service | Why |
|---|---|---|
| Kafka + Zookeeper | **Azure Event Hubs** (Kafka endpoint) | Managed, Kafka-protocol-compatible broker — the producer/processor/consumer keep using `bootstrap.servers`, only the endpoint and SASL config change. |
| PostgreSQL container | **Azure Database for PostgreSQL Flexible Server** | Managed Postgres with backups and HA. Oracle estate? See [Oracle on Azure](#oracle-on-azure) below. |
| Redis container | **Azure Cache for Redis** | Managed cache for the cache-aside read path. |
| In-process SignalR | **Azure SignalR Service** | A backplane so the WebSocket channel survives horizontal scale-out of the backend. Wired automatically when `ConnectionStrings__AzureSignalR` is present (see `Program.cs`). |
| `backend`, `processor`, `producer`, `frontend` containers | **Azure Container Apps** | Serverless containers with scale rules, revisions, and log streaming into Log Analytics. |

## Files

- **`main.bicep`** — infrastructure as code: Log Analytics, Event Hubs (Kafka), PostgreSQL
  Flexible Server, Redis, SignalR Service, a Container Apps environment, and the backend
  container app wired to all of them via secrets. Illustrative — it provisions the backend
  as the fully-wired exemplar; the other three apps follow the same `containerApps` shape.
- **`main.parameters.json`** — parameter values; the DB password is pulled from Key Vault
  rather than committed.

## Deploy

```bash
# 0. Build and push images to your registry (see the ci-cd workflow, or by hand):
az acr build -r <acr> -t motorvalley-backend:$(git rev-parse --short HEAD) ./backend

# 1. Validate the template
az bicep build --file deploy/azure/main.bicep

# 2. Deploy into a resource group
az group create -n rg-motorvalley -l westeurope
az deployment group create \
  -g rg-motorvalley \
  -f deploy/azure/main.bicep \
  -p acrLoginServer=<acr>.azurecr.io \
     imageTag=$(git rev-parse --short HEAD) \
     postgresAdminPassword=<secret>
```

The deployment outputs the backend's public FQDN.

## Oracle on Azure

The JD-side database standard is Oracle, and the backend is provider-agnostic through EF Core
(`Database:Provider = Oracle`, see the root README). On Azure the managed options are
**Oracle Database@Azure** (Oracle Exadata, first-party in the portal) or Oracle running on an
Azure VM. The application change is a connection string and generating the Oracle migration set
(`dotnet ef migrations add InitialCreate` with the provider set to Oracle) — no repository or
domain code changes, which is the point of keeping persistence behind `IAlertRepository`.

## Notes

- This IaC is illustrative and cost-conscious (Burstable Postgres, Basic Redis) — it is meant
  to demonstrate the deployment topology and the local→cloud mapping, not to be a hardened
  production template. Production would add private networking (VNet integration + private
  endpoints), managed identity for data-plane auth instead of connection-string secrets, and
  zone-redundant SKUs.
