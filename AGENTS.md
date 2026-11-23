# PengSim repository conventions

- Use project classes through interfaces. Use interface types for dependencies, return values, and collections of model objects. Reference implementations at construction or registration sites. Coordinates, pixels, and other value types can be used directly.
- Order members within each C# type as follows: fields and constants, properties and indexers, constructors, public methods, then private methods. Group explicit interface methods with the methods that callers use. Primary constructors belong in the type declaration.
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
