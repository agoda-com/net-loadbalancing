
| Build | Status |
|--------|--------|
| Project | [![Build status](https://ci.appveyor.com/api/projects/status/l72mp8rhenjc9n42?svg=true)](https://ci.appveyor.com/project/jenol/net-loadbalancing) | 
| Master | [![Build status](https://ci.appveyor.com/api/projects/status/l72mp8rhenjc9n42/branch/master?svg=true)](https://ci.appveyor.com/project/jenol/net-loadbalancing/branch/master) | 

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

```
dotnet add Agoda.Frameworks.Http
```

### DB

```
dotnet add Agoda.Frameworks.DB
```

### LoadBalancing Core

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

