# Weather Station Images Generator

[![Azure Functions](https://img.shields.io/badge/Azure-Functions-blue?style=flat&logo=azure-functions)](https://azure.microsoft.com/en-us/products/functions/)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=.net)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Azure DevOps](https://img.shields.io/badge/Azure%20DevOps-Pipeline-0078D7?style=flat&logo=azure-devops)](https://azure.microsoft.com/en-us/products/devops/)

A serverless application that generates weather visualization images for Dutch weather stations using Azure Functions and the Buienradar API.

## 🌟 Features

- **Real-time Weather Data**: Integration with Buienradar API for live Dutch weather information
- **Parallel Processing**: Asynchronous image generation using Azure Queue Storage
- **Serverless Architecture**: Scalable Azure Functions implementation
- **Continuous Deployment**: Automated deployment pipeline using Azure DevOps
- **Status Tracking**: Real-time progress monitoring through REST API
- **Infrastructure as Code**: Complete Azure infrastructure defined in Bicep

## 🏗️ Architecture

```mermaid
graph LR
    A[HTTP Trigger] --> B[Queue Storage]
    B --> C[Queue Trigger]
    C --> D[Blob Storage]
    E[Status API] --> F[Table Storage]
    C -.-> E
```

## 🚀 Tech Stack

- **.NET 8.0** - Latest LTS version
- **Azure Functions** - Serverless compute
- **Azure Storage** - Queues, Blobs, and Tables
- **Azure DevOps** - CI/CD Pipeline
- **Bicep** - Infrastructure as Code
- **SixLabors.ImageSharp** - Image processing

## 💡 Key Design Principles

- **Serverless Architecture**: Pay-per-use model with automatic scaling
- **Asynchronous Processing**: Non-blocking operations for better performance
- **SOLID Principles**: Clean code architecture with dependency injection
- **Infrastructure as Code**: Reproducible deployments using Bicep
- **Monitoring & Logging**: Application Insights integration

## 📊 API Endpoints

```http
# Generate Weather Images
GET /api/CreateImages

# Check Generation Status
GET /api/status/{jobId}
```

## 🛠️ Local Development Requirements

- Azurite
- .NET 8.0 SDK
- Cosmos DB Emulator
- Azure Storage Explorer   
- Visual Studio 2022 (recommended)

## 🚢 Deployment

```powershell
# Deploy infrastructure
./infrastructure/deploy.ps1 -resourceGroupName "weather-rg" -location "westeurope"
```

## 🔗 Links

- [Buienradar API](https://www.buienradar.nl/overbuienradar/gratis-weerdata)