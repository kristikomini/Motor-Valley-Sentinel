// Motor Valley Sentinel — Azure infrastructure (illustrative IaC).
//
// Maps the local docker-compose stack onto Azure PaaS: the five services run as
// Azure Container Apps, and the backing infrastructure (broker, database, cache,
// realtime transport) becomes managed services instead of self-hosted containers.
//
//   Kafka       -> Azure Event Hubs (Kafka-compatible endpoint)
//   PostgreSQL  -> Azure Database for PostgreSQL Flexible Server
//   Redis       -> Azure Cache for Redis
//   SignalR     -> Azure SignalR Service (scale-out backplane)
//   containers  -> Azure Container Apps (built images pulled from ACR)
//
// Validate:  az bicep build --file main.bicep
// Deploy:    az deployment group create -g <rg> -f main.bicep -p @main.parameters.json

targetScope = 'resourceGroup'

@description('Short prefix for resource names, e.g. "mvsentinel".')
@minLength(3)
@maxLength(16)
param namePrefix string = 'mvsentinel'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Container image tag to deploy (e.g. a git SHA).')
param imageTag string = 'latest'

@description('Login server of the Azure Container Registry holding the images (e.g. myacr.azurecr.io).')
param acrLoginServer string

@description('PostgreSQL administrator login.')
param postgresAdminUser string = 'mvadmin'

@description('PostgreSQL administrator password.')
@secure()
param postgresAdminPassword string

var tags = {
  project: 'motor-valley-sentinel'
  managedBy: 'bicep'
}

// ---------------------------------------------------------------------------
// Observability — Log Analytics backs the Container Apps environment.
// ---------------------------------------------------------------------------
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-logs'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

// ---------------------------------------------------------------------------
// Broker — Event Hubs namespace with a Kafka surface; one hub per topic.
// ---------------------------------------------------------------------------
resource eventHubs 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: '${namePrefix}-eh'
  location: location
  tags: tags
  sku: {
    name: 'Standard' // Kafka endpoint requires Standard or above
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    kafkaEnabled: true
  }
}

resource sensorDataHub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  parent: eventHubs
  name: 'sensor-data'
  properties: {
    partitionCount: 4
    messageRetentionInDays: 1
  }
}

resource criticalAlertsHub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  parent: eventHubs
  name: 'critical-alerts'
  properties: {
    partitionCount: 4
    messageRetentionInDays: 1
  }
}

// ---------------------------------------------------------------------------
// Relational store — PostgreSQL Flexible Server (Oracle Database@Azure is the
// swap for an Oracle-standardised estate; see deploy/azure/README.md).
// ---------------------------------------------------------------------------
resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: '${namePrefix}-pg'
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: postgresAdminUser
    administratorLoginPassword: postgresAdminPassword
    storage: { storageSizeGB: 32 }
    backup: { backupRetentionDays: 7 }
    highAvailability: { mode: 'Disabled' }
  }
}

resource postgresDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: 'motorvalley'
}

// Allow other Azure services (the Container Apps) to reach the server.
resource postgresFirewall 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgres
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---------------------------------------------------------------------------
// Cache — Azure Cache for Redis (cache-aside read path).
// ---------------------------------------------------------------------------
resource redis 'Microsoft.Cache/redis@2024-03-01' = {
  name: '${namePrefix}-redis'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

// ---------------------------------------------------------------------------
// Realtime — Azure SignalR Service backplane for the dashboard push channel.
// ---------------------------------------------------------------------------
resource signalr 'Microsoft.SignalRService/signalR@2024-03-01' = {
  name: '${namePrefix}-signalr'
  location: location
  tags: tags
  sku: {
    name: 'Standard_S1'
    capacity: 1
  }
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Default'
      }
    ]
  }
}

// ---------------------------------------------------------------------------
// Container Apps environment — shared runtime for the five services.
// ---------------------------------------------------------------------------
resource caeEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${namePrefix}-cae'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// Connection strings assembled from the managed services above.
var redisConnString = '${redis.name}.redis.cache.windows.net:6380,password=${redis.listKeys().primaryKey},ssl=True,abortConnect=False'
var pgConnString = 'Host=${postgres.properties.fullyQualifiedDomainName};Port=5432;Database=motorvalley;Username=${postgresAdminUser};Password=${postgresAdminPassword};SslMode=Require'
var kafkaBootstrap = '${eventHubs.name}.servicebus.windows.net:9093'
var signalrConnString = signalr.listKeys().primaryConnectionString

// The .NET backend — public ingress, wired to every managed service.
resource backendApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${namePrefix}-backend'
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: caeEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 5000
        transport: 'auto'
      }
      registries: [
        {
          server: acrLoginServer
          identity: 'system'
        }
      ]
      secrets: [
        { name: 'pg-conn', value: pgConnString }
        { name: 'redis-conn', value: redisConnString }
        { name: 'signalr-conn', value: signalrConnString }
      ]
    }
    template: {
      containers: [
        {
          name: 'backend'
          image: '${acrLoginServer}/motorvalley-backend:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'ASPNETCORE_URLS', value: 'http://0.0.0.0:5000' }
            { name: 'Database__Provider', value: 'Postgres' }
            { name: 'ConnectionStrings__PostgreSQL', secretRef: 'pg-conn' }
            { name: 'ConnectionStrings__Redis', secretRef: 'redis-conn' }
            { name: 'ConnectionStrings__AzureSignalR', secretRef: 'signalr-conn' }
            { name: 'Kafka__BootstrapServers', value: kafkaBootstrap }
            { name: 'Kafka__AlertsTopic', value: 'critical-alerts' }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output backendFqdn string = backendApp.properties.configuration.ingress.fqdn
output eventHubsNamespace string = eventHubs.name
output postgresFqdn string = postgres.properties.fullyQualifiedDomainName
