# Agoda.Frameworks.LoadBalancing

Agoda.Frameworks.LoadBalancing is a library that provides logic for handling retry and load balancing.

## Features

- Weight-adjusted random selection for data sources
- Retry mechanism for sync and async functions
- Thread-safe and multi-threading friendly retry manager
- Dynamic weight adjustment base on action results
- Built-in support for various weight manipulation strategies (fixed delta, exponential, etc.)
- Built-in events for retry manager

## Managing Resources

```
// Initialize resource manager
var mgr = new ResourceManager<string>(new Dictionary<string, WeightItem>()
{
    ["src1"] = new WeightItem(100, 100),
    ["src2"] = new WeightItem(100, 100)
}, new FixedDeltaWeightManipulationStrategy(20));

// Select resource randomly and execute action
mgr.ExecuteAction(src => {
  // src is either "src1" or "src2"
  // source is randomly chosen based on weight
  ...
}, (attemptCount, exception) => {
  // predicate for determine whether or not to retry
  // return true for retry, return false to throw exception

  // Rethrows at the 3rd attempt.
  return attemptCount < 3;
})

// Asnyc version of ExecuteAction, always use it for async operation.
mgr.ExecuteAsync(async src => ..., ...)

// Update new collection of resources
// src1 will be kept, src2 will be removed and src3 will be added
mgr.UpdateResources(new Dictionary<string, WeightItem>()
{
    ["src1"] = new WeightItem(100, 100),
    ["src3"] = new WeightItem(100, 100)
});
```

## Dynamic Weight Management

By calling `ExecuteAction` or `ExecuteAsync`, ResourceManager updates the weight of resources automatically by looking at the result of executed action. ResourceManager is thread-safe and locks only when it's updating weights for resources.

Example:

```
// Uses fixed delta strategy with delta = 20
var mgr = new ResourceManager<string>(new Dictionary<string, WeightItem>()
{
    ["src1"] = new WeightItem(100, 100),
    ["src2"] = new WeightItem(100, 100)
}, new FixedDeltaWeightManipulationStrategy(20));

// ExecuteAction 1st time, chances of getting src1 and getting src2 are both 50%.
// Assume src1 got selected and action failed, src1's weight became 80.
mgr.ExecuteAction(...);

// ExecuteAction 2nd time, chance of getting src1 is 45%, and chance of
// getting src2 is 55% due to previous weight change. (80/180 & 100/180)
// Assume src2 got selected and action succeeded, src2's weight would still
// remain 100 due to weight cap is 100.
mgr.ExecuteAction(...);
```

### FixedDeltaWeightManipulationStrategy

Update weight for selected source by adding or decreasing fixed value to original weight.

### ExponentialWeightManipulationStrategy

Update weight for selected source by multiplying magnitude to original weight.

### SplitWeightManipulationStrategy

Weight manipulation wrapper for setting different strategies for success and unsuccess.

### NoopWeightManipulationStrategy

Do nothing.
