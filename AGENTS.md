# PengSim repository conventions

- Use project classes through interfaces. Use interface types for dependencies, return values, and collections of model objects. Reference implementations at construction or registration sites. Coordinates, pixels, and other value types can be used directly.
- Order members within each C# type as follows: fields and constants, properties and indexers, constructors, public methods, then private methods. Group explicit interface methods with the methods that callers use. Primary constructors belong in the type declaration.
- Keep the temporary inward movement rule in a private, demo-named method on Penguin. Call it from the IPenguin.Move implementation.
- Update src/Doc/ai_doc.md when behavior or design changes. Record what changed and why. Keep the README’s render-profiling command current when profiling changes.
