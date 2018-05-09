## Overview

Agoda.LoadBalancing is a .NET Standard library that provides logic for handling retry and load balancing.

## Features

- Weight-adjusted random selection for data sources
- Retry mechanism for sync and async functions
- Thread-safe and multi-threading friendly retry manager
- Dynamic weight adjustment base on action results
- Built-in support for various weight manipulation strategies (fixed delta, exponential, etc.)
- Built-in events for retry manager

## Install

```
dotnet add Agoda.LoadBalancing
```

## Build

```
dotnet build
```

## Test

```
dotnet test Agoda.LoadBalancing.Test/Agoda.LoadBalancing.Test.csproj
```
