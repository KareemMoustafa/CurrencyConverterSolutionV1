# Currency Converter API

## Overview

The Currency Converter API is a robust, scalable, and maintainable API built with **C# and ASP.NET Core 8**. It enables currency conversion, retrieval of latest exchange rates, and historical exchange rates with pagination while ensuring high security, resilience, and performance.

## Features

- **Retrieve Latest Exchange Rates**: Fetches real-time exchange rates from the Frankfurter API.
- **Currency Conversion**: Converts amounts between different currencies while excluding TRY, PLN, THB, and MXN.
- **Historical Exchange Rates**: Retrieves past exchange rates with pagination support.
- **Resilience & Performance**: Implements caching, retry policies, and circuit breakers.
- **Security & Access Control**: Utilizes JWT authentication, RBAC, and API throttling.
- **Logging & Monitoring**: Incorporates structured logging and distributed tracing.
- **Testing & CI/CD**: High test coverage with unit and integration tests.
- **Scalability & Deployment**: API versioning and horizontal scaling support.

## Setup Instructions

### Prerequisites

- .NET Core 8 SDK
- Visual Studio or VS Code
- Docker (optional, for containerized deployment)
- PostgreSQL / SQL Server (for database, if applicable)
- API key for the Frankfurter API (if needed)

### Installation

1. Clone the repository:
   ```sh
   git clone https://github.com/KareemMoustafa/CurrencyConverterSolutionV1.git
   cd CurrencyConverterAPI
   ```
2. Restore dependencies:
   ```sh
   dotnet restore
   ```
3. Configure environment variables in `appsettings.json` or use `secrets.json` for:
   - Base currency
   - API key (if required)
   - JWT settings
4. Run the API:
   ```sh
   dotnet run
   ```
5. Access the Swagger UI at:
   ```sh
   http://localhost:5000/swagger
   ```

## Assumptions Made

- The API will primarily fetch data from the Frankfurter API.
- The base currency is set to **EUR** by default.
- TRY, PLN, THB, and MXN currencies are not allowed due to business rules.
- JWT authentication is required for all API requests.
- Caching is implemented to minimize API calls to the external provider.
- Logging and monitoring are enabled for debugging and performance tracking.

## Possible Future Enhancements

- **Support for Additional Exchange Rate Providers**: Implement a provider factory to integrate multiple data sources.
- **More Currency Filters**: Allow configuration-based exclusions instead of hardcoded ones.
- **WebSocket Support**: Real-time updates for exchange rates.
- **GraphQL Support**: Improve API flexibility for client requests.
- **User Interface**: Develop a frontend dashboard for easy monitoring and usage.
- **Database Storage**: Store historical data locally for better performance and analysis.
- **Auto-Scaling & Cloud Deployment**: Deploy on **Azure Kubernetes Service (AKS)** or AWS Lambda for better scaling.

## API Endpoints

### 1. Retrieve Latest Exchange Rates

**GET** `/api/rates/api/exchange-rates/latest?baseCurrency=EUR&provider=FrankfurterExchangeRateProvider`

### 2. Convert Currency

**GET** `/api/exchange-rates/convert?from=EUR&provider=FrankfurterExchangeRateProvider&to=USD`

### 3. Retrieve Historical Exchange Rates

**GET** `/api/exchange-rates/historical?startDate=2025-01-12&endDate=2025-02-14&page=1&pageSize=5&baseCurrency=EUR&provider=FrankfurterExchangeRateProvider`

## Contributing

1. Fork the repository.
2. Create a feature branch: `git checkout -b feature-name`
3. Commit changes: `git commit -m "Add new feature"`
4. Push to the branch: `git push origin feature-name`
5. Create a Pull Request.

## License

This project is licensed under the .

## Contact

For any inquiries, reach out to **kalx.87@gmail.com** or open an issue on GitHub.

---
