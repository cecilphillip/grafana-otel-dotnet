# ASP.NET Core observability w/ Grafana OSS Stack 

Sample application showing how to write up ASP.NET Core with OpenTelemetry and export to Prometheus, Loki, and Tempo using the OpenTelemetry Collector 


## Prerequisites

* [.NET SDK v7.0+](https://get.dot.net/)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Setup
The demo is made up of a few projects written in C#.
* [Web API](src/BackendApiService) - Backend HTTP Weather API built with ASP.NET Core
* [Website](src/MainService) - Front facing UI using the default Blazor Server template

### Configured Ports 🔌
| Application             | Port(s)    |
|-------------------------|------------|
| Web API                 | 5006       |
| Website                 | 5238       |
| Loki                    | 3100       |
| Prometheus UI           | 9090       |
| Grafana UI              | 3000       |
| OpenTelemetry Collector | 4317, 4318 |


### Running the demo 🚀
Run the infrastructure components with the provided [docker-compose.yml](./docker-compose.yml) file.

```shell
>  docker compose -p grafana-demo up   
```
> 👀 This setup will create a **./tmp/** folder in the root of the project folder that will be mounted as a volume 
> in the infrastructure containers.   

Start both .NET applications

Backend Web API
```shell
> cd src/BackendApiService
> dotnet run 
```
Front facing web API
```shell
> cd src/MainService
> dotnet run 
```
