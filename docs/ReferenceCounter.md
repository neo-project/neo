# ReferenceCounter

`ReferenceCounter` is a reference counting manager for use in a virtual machine (VM). It is designed to track and manage the reference counts of objects to ensure they are correctly cleaned up when no longer referenced, thereby preventing memory leaks.

## Purpose

In a virtual machine, managing object memory is crucial. The main purposes of `ReferenceCounter` are:

1. **Tracking Object References**: Manage and track the reference relationships between objects.
2. **Memory Management**: Ensure that objects are correctly released when they are no longer referenced.
3. **Preventing Memory Leaks**: Avoid memory leaks by using reference counting and detecting circular references.

## Technical Principles

### Reference Counting

Reference counting is a memory management technique used to track the number of references to each object. When an object's reference count drops to zero, it indicates that the object is no longer in use and can be safely cleaned up. `ReferenceCounter` uses the principles of reference counting to manage the lifecycle of objects:

- **Increment Reference Count**: Increase the reference count when an object is referenced.
- **Decrement Reference Count**: Decrease the reference count when a reference is removed.
- **Cleanup Object**: Cleanup the object when its reference count drops to zero.

### What is Tracked

In the Neo VM, the `ReferenceCounter` class is used to count references to objects to track and manage `StackItem` references. The `reference_count` calculates the total number of current references, including stack references and object references. Specifically, the `reference_count` increases or decreases in the following situations:

#### Increment Reference

Use the `AddReference` method to increment the reference count of an object:

```csharp
internal void AddReference(StackItem item, CompoundType parent)
{
    references_count++;
    if (!NeedTrack(item)) return;
    cached_components = null;
    tracked_items.Add(item);
    item.ObjectReferences ??= new(ReferenceEqualityComparer.Instance);
    if (!item.ObjectReferences.TryGetValue(parent, out var pEntry))
    {
        pEntry = new(parent);
        item.ObjectReferences.Add(parent, pEntry);
    }
    pEntry.References++;
}
```

#### Decrement Reference

Use the `RemoveReference` method to decrement the reference count of an object:

```csharp
internal void RemoveReference(StackItem item, CompoundType parent)
{
    references_count--;
    if (!NeedTrack(item)) return;
    cached_components = null;
    item.ObjectReferences![parent].References--;
    if (item.StackReferences == 0)
        zero_referred.Add(item);
}
```

#### Increment Stack Reference

Use the `AddStackReference` method to increment the stack reference count of an object:

```csharp
internal void AddStackReference(StackItem item, int count = 1)
{
    references_count += count;
    if (!NeedTrack(item)) return;
    if (tracked_items.Add(item))
        cached_components?.AddLast(new HashSet<StackItem>(ReferenceEqualityComparer.Instance) { item });
    item.StackReferences += count;
    zero_referred.Remove(item);
}
```

#### Decrement Stack Reference

Use the `RemoveStackReference` method to decrement the stack reference count of an object:

```csharp
internal void RemoveStackReference(StackItem item)
{
    references_count--;
    if (!NeedTrack(item)) return;
    if (--item.StackReferences == 0)
        zero_referred.Add(item);
}
```

### Circular References

Circular references occur when objects reference each other, preventing their reference counts from dropping to zero, which can lead to memory leaks. `ReferenceCounter` addresses circular references using the following methods:

1. **Mark and Sweep**: Detect and clean up strongly connected components when circular references are identified using algorithms like Tarjan's algorithm.
2. **Recursive Reference Management**: Recursively manage the reference counts of nested objects to ensure all reference relationships are correctly handled.

### Tarjan's Algorithm

Tarjan's algorithm is a graph theory algorithm for finding strongly connected components (SCCs) in a directed graph. An SCC is a maximal subgraph where every vertex is reachable from every other vertex in the subgraph. In the context of `ReferenceCounter`, Tarjan's algorithm is used to detect circular references, allowing for efficient memory management and cleanup of objects that are no longer reachable.

#### How Tarjan's Algorithm Works

1. **Initialization**:
    - Each node (object) in the graph is initially unvisited. The algorithm uses a stack to keep track of the current path and arrays (or lists) to store the discovery time (`DFN`) and the lowest point reachable (`LowLink`) for each node.

2. **Depth-First Search (DFS)**:
    - Starting from an unvisited node, the algorithm performs a DFS. Each node visited is assigned a discovery time and a `LowLink` value, both initially set to the node's discovery time.

3. **Update LowLink**:
    - For each node, the algorithm updates the `LowLink` value based on the nodes reachable from its descendants. If a descendant node points back to an ancestor in the current path (stack), the `LowLink` value of the current node is updated to the minimum of its own `LowLink` and the descendant's `LowLink`.

4. **Identify SCCs**:
    - When a node's `LowLink` value is equal to its discovery time, it indicates the root of an SCC. The algorithm then pops nodes from the stack until it reaches the current node, forming an SCC.

5. **Cleanup**:
    - Once SCCs are identified, nodes that have no remaining references are cleaned up, preventing memory leaks caused by circular references.

### Tarjan's Algorithm in `ReferenceCounter`

The `CheckZeroReferred` method in `ReferenceCounter` uses Tarjan's algorithm to detect and handle circular references. Here’s a detailed breakdown of the algorithm as used in `CheckZeroReferred`:

```csharp
internal int CheckZeroReferred()
{
    // If there are items with zero references, process them.
    if (zero_referred.Count > 0)
    {
        // Clear the zero_referred set since we are going to process all of them.
        zero_referred.Clear();

        // If cached components are null, we need to recompute the strongly connected components (SCCs).
        if (cached_components is null)
        {
            // Create a new Tarjan object and invoke it to find all SCCs in the tracked_items graph.
            Tarjan tarjan = new(tracked_items);
            cached_components = tarjan.Invoke();
        }

        // Reset all tracked items' Tarjan algorithm-related fields (DFN, LowLink, and OnStack).
        foreach (StackItem item in tracked_items)
            item.Reset();

        // Process each SCC in the cached_components list.
        for (var node = cached_components.First; node != null;)
        {
            var component = node.Value;
            bool on_stack = false;

            // Check if any item in the SCC is still on the stack.
            foreach (StackItem item in component)
            {
                // An item is considered 'on stack' if it has stack references or if its parent items are still on stack.
                if (item.StackReferences > 0 || item.ObjectReferences?.Values.Any(p => p.References > 0 && p.Item.OnStack) == true)
                {
                    on_stack = true;
                    break;
                }
            }

            // If any item in the component is on stack, mark all items in the component as on stack.
            if (on_stack)
            {
                foreach (StackItem item in component)
                    item.OnStack = true;
                node = node.Next;
            }
            else
            {
                // Otherwise, remove the component and clean up the items.
                foreach (StackItem item in component)
                {
                    tracked_items.Remove(item);

                    // If the item is a CompoundType, adjust the reference count and clean up its sub-items.
                    if (item is CompoundType compound)
                    {
                        // Decrease the reference count by the number of sub-items.
                        references_count -= compound.SubItemsCount;
                        foreach (StackItem subitem in compound.SubItems)
                        {
                            // Skip sub-items that are in the same component or don't need tracking.
                            if (component.Contains(subitem)) continue;
                            if (!NeedTrack(subitem)) continue;

                            // Remove the parent reference from the sub-item.
                            subitem.ObjectReferences!.Remove(compound);
                        }
                    }

                    // Perform cleanup for the item.
                    item.Cleanup();
                }

                // Move to the next component and remove the current one from the cached_components list.
                var nodeToRemove = node;
                node = node.Next;
                cached_components.Remove(nodeToRemove);
            }
        }
    }

    // Return the current total reference count.
    return references_count;
}
```

### Detailed Explanation

1. **Initialization and Check for Zero References**:
    - The method starts by checking if there are any items in `zero_referred`. If there are, it clears the `zero_referred` set.

2. **Compute Strongly Connected Components (SCCs)**:
    - If there are no cached SCCs, it recomputes them using Tarjan's algorithm. This involves creating a `Tarjan` object, passing the `tracked_items` to it, and invoking the algorithm to get the SCCs.

3. **Reset Tarjan-related Fields**:
    - It resets the Tarjan-related fields (`DFN`, `LowLink`, `OnStack`) of all tracked items to prepare for processing SCCs.

4. **Process Each SCC**:
    - It iterates through each SCC (component) in the cached components list. For each component, it checks if any item is still on the stack by looking at its `StackReferences` or if any of its parent items are on the stack.

5. **Mark Items as On Stack**:
    - If any item in the component is still on the stack, it marks all items in the component as on the stack and moves to the next component.

6. **Remove and Clean Up Items**:
    - If no items in the component are on the stack, it removes the component and cleans up all items within it. For `CompoundType` items, it adjusts the reference count and removes parent references from their sub-items. It then performs cleanup on each item and removes the component from the cached components list.

7. **Return Reference Count**:
    - Finally, it returns the current total reference count.

## Features

`ReferenceCounter` provides the following features:

1. **Increment Reference Count**: Increment the reference count of objects.
2. **Decrement Reference Count**: Decrement the reference count of objects.
3. **Check Zero-Referenced Objects**: Detect and clean up objects with a reference count of zero.
4. **Manage Nested References**: Recursively manage the reference counts of nested objects, supporting nested arrays of arbitrary depth.
