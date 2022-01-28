![](https://github.com/agoda-com/net-loadbalancing/workflows/.NET%20Core%20Build%20and%20Deploy/badge.svg)

## Overview

Agoda.Frameworks.LoadBalancing is a .NET Standard library that provides logic for handling retry and load balancing.

## Features

- Weight-adjusted random selection for data sources
- Retry mechanism for sync and async functions
- Thread-safe and multi-threading friendly retry manager
- Dynamic weight adjustment base on action results
- Built-in events for retry manager

## Documentations

[docs](./docs)

## Install

### Http
[![NuGet version](https://badge.fury.io/nu/Agoda.Frameworks.Http.svg)](https://badge.fury.io/nu/Agoda.Frameworks.Http)

```
dotnet add Agoda.Frameworks.Http
```

### DB
[![NuGet version](https://badge.fury.io/nu/Agoda.Frameworks.DB.svg)](https://badge.fury.io/nu/Agoda.Frameworks.DB)

```
dotnet add Agoda.Frameworks.DB
```

### Grpc
[![NuGet version](https://badge.fury.io/nu/Agoda.Frameworks.Grpc.svg)](https://badge.fury.io/nu/Agoda.Frameworks.Grpc)

```
dotnet add Agoda.Frameworks.Grpc
```

### LoadBalancing Core
[![NuGet version](https://badge.fury.io/nu/Agoda.Frameworks.LoadBalancing.svg)](https://badge.fury.io/nu/Agoda.Frameworks.LoadBalancing)

```
dotnet add Agoda.Frameworks.LoadBalancing
```

## Build

```
dotnet build
```

## Test

```
dotnet test
```

## Contributors

@cyberhck for adding support for GitHub actions and fixed unit tests under different locales