# PengSim repository conventions

- Reserve the `Contracts` folder and namespace for interfaces. Place concrete domain types in `Model`.
- Use project classes through interfaces. Use interface types for dependencies, return values, and collections of model objects. Reference implementations at construction or registration sites. Coordinates, pixels, and other value types can be used directly.
- Document every interface and every member it declares with an XML `<summary>`, including internal interfaces, constants, properties, indexers, and methods. Describe the contract from the caller’s perspective.
- Order members within each C# type as follows: fields and constants, properties and indexers, constructors, public methods, then private methods. Group explicit interface methods with the methods that callers use. Primary constructors belong in the type declaration.
- Use `ushort` for board width and height, penguin IDs, and stored offset coordinates (X and Y). Enforce at most 65,000 penguins and dimensions of 1–65,000 on each axis. Keep signed arithmetic for intermediate hex geometry and validate bounds before converting to `ushort`.
- Store iteration history as read-only position arrays in consecutive ID order, with ID 1 at index 0. Allocate four coordinate bytes per penguin per completed iteration. Keep occupancy indexes for working state or the selected view only. Never store full 2D grids or dictionaries per iteration.
- Keep the temporary inward movement rule in a private, demo-named method on Penguin. Call it from the IPenguin.Move implementation.
- Update src/Doc/ai_doc.md when behavior or design changes. Record what changed and why. Keep the README’s render-profiling command current when profiling changes.

## C# readability and maintainability

- Use `var` for local variables when the compiler can infer the intended type. Declare an interface type explicitly when inference would expose the concrete implementation.
- Give every method implementation a brace-delimited block body, including single-statement methods and explicit interface implementations. Put the braces and statements on separate lines. Interface and abstract method declarations can end with a semicolon.
- Always use braces for `if`, `else`, `for`, `foreach`, `while`, and `do` bodies. This includes early returns, throws, and loop exits. Put each statement on its own line.
- Keep each class focused on one responsibility and inject only the dependencies it uses. If separate groups of methods use separate groups of dependencies, split the responsibilities into cohesive classes. Keep dependencies visible in the constructor.
- Keep methods focused and control flow shallow. Use guard clauses for invalid inputs and early exits. Extract a named helper when a block performs a distinct operation.
- Use names that describe intent. Give domain thresholds and repeated settings meaningful names. Remove unused members, unused dependencies, and commented-out code.
- Catch exceptions where the caller can recover or add useful context. Use `throw;` when rethrowing the same exception to preserve its stack trace. Use `using` or `await using` for disposable resources owned by the current scope.

## ReSharper inspection guidance

- When a reference can be null, reflect that in its type and handle the missing case before dereferencing it. For required inputs, validate at the boundary. [Possible null reference](https://www.jetbrains.com/help/resharper/PossibleNullReferenceException.html)
- When processing a deferred `IEnumerable<T>`, check whether a preliminary `Any()` or `Count()` causes a second traversal. Combine the work into one pass when possible. Materialize once when callers need to revisit the same snapshot. [Possible multiple enumeration](https://www.jetbrains.com/help/resharper/PossibleMultipleEnumeration.html)
- When a callback needs the frame or loop index from the moment it was created, capture a local copy. When it uses a disposable resource, ensure the resource stays alive until the callback completes. [Modified captures](https://www.jetbrains.com/help/resharper/AccessToModifiedClosure.html) · [Disposed captures](https://www.jetbrains.com/help/resharper/AccessToDisposedClosure.html)
- Investigate conditions that always evaluate to the same result. Check the intended boundary cases before simplifying a branch, especially for grid bounds and playback indices.
- Apply quick-fixes that preserve behavior and the conventions above. Keep explicit interfaces, block-bodied methods, and braced control flow when the IDE offers a shorter form.
