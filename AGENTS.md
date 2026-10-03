# Agent Instructions

## Change Authorization

- Do not modify any file unless the user explicitly asks for a change.
- Requests to inspect, diagnose, explain, review, or identify a cause authorize read-only investigation only.
- Do not infer permission to implement a fix from a reported bug or problem.
- After a read-only investigation, describe the proposed change and wait for explicit user authorization before editing files.

## Change Scope and Rationale

- Even though this project is under active development, do not casually change anything the user did not explicitly request. Preserve existing behavior, structure, APIs, and surrounding code unless the requested work makes a change necessary or there is a clear, concrete reason for it.
- Avoid unrelated refactoring, cleanup, style changes, or opportunistic behavior changes. Keep changes minimal and within the user's requested scope.
- This is not an absolute prohibition: when the user's request necessarily requires a structural change, make that necessary change directly. Do not force an unnecessary workaround merely to avoid changing the structure.
- If a change outside the explicit request is necessary, explain what changed and why. Always tell the user about structural, behavioral, or contract changes, especially any public API or other contract change, including the rationale and impact.

## Change Disclosure

- Do not make silent behavior changes while fixing or migrating code.
- If you notice and fix an issue outside the user's explicit request, call it out clearly in the response.
- This includes changes that look obviously correct, such as changing file-system operations to use a provider root (`targetPath + path`) instead of the raw path.
- Explain why the change was made, what behavior it changes, and that it can be reverted if the previous behavior was intentional.

## Public API Compatibility

- This project is under active development. Public API contracts do not need to be preserved solely for compatibility; break them when necessary.
- When breaking or materially changing a public API contract, explicitly explain the decision, the changed contract, and the resulting impact in the response.

## Language

- Unless the user explicitly asks for another language, respond in Korean by default.
- Even if the user writes in English, respond in Korean by default unless the user explicitly asks to change languages.

## Project Context

- This project is a Unity/C# project.
- When the user asks a question that requires understanding this project's structure, do not guess the structure from imagination. Inspect the actual files first, usually under the `/Packages` folder.
- Before any C# investigation or change, inspect the relevant assembly's `GenericGlobalUsing`, `GenericEditorGlobalUsing`, and `AssemblyInfo` files. Decide whether a namespace may be omitted and whether `internal` access is available only from those live files; never infer either from convention.

## Verification

- Do not run .NET build commands such as `dotnet build`, `dotnet test`, or generated `.csproj`/`.sln` builds for verification.
- Do not run syntax-only compile checks unless the user explicitly asks.
- For Unity/C# changes, prefer lightweight inspection such as reading the changed files and checking the intended API usage.

## C# Style

- Put `#nullable enable` at the very top of C# files.

## Standalone RuniOS.CodeAnalysis

- `RuniOS.CodeAnalysis` is a standalone Roslyn/.NET project, independent of the Unity runtime and editor projects.
- Do not apply Unity-specific C# conventions to files under `RuniOS.CodeAnalysis`: do not add a file-level `#nullable enable` directive or block-scoped namespace declarations. Preserve the existing file-scoped namespace style.
- The general C# rule requiring `#nullable enable` does not apply to `RuniOS.CodeAnalysis`. Its project-level nullable configuration remains authoritative.
- For `RuniOS.CodeAnalysis`, inspect its own project and source configuration instead of requiring Unity assembly files such as `GenericGlobalUsing`, `GenericEditorGlobalUsing`, or `AssemblyInfo`.

## Code design philosophy

Prefer the smallest implementation that expresses the intended contract.

Do not add behavior, policy, validation, abstraction, state, or lifecycle management unless it is required by the existing design or fixes a concrete demonstrated problem.

### Preserve intentional freedom

Do not make unspecified behavior deterministic merely for reproducibility.

For example:

- If objects are sorted by `order`, compare only `order`.
- Do not add type names, assembly names, registration order, GUIDs, or other tie-breakers unless the code explicitly requires a total ordering.
- Equal priority means equal priority. Their relative order may remain unspecified.
- Do not clamp or validate values merely because they look unusual if the current representation can validly express them.

Undefined or unconstrained behavior is not automatically a bug.

### Do not invent contracts

Do not infer new requirements from implementation details.

A field name such as `index`, a collection boundary, a type name, or the presence of a lifecycle callback does not by itself imply additional validation or semantics.

Before adding a rule, ask:

1. Is this rule already part of the code's contract?
2. Is it required for correctness?
3. Is there a concrete failure without it?

If all answers are no, do not add it.

### Avoid defensive overengineering

Do not introduce extra:

- state flags
- lifecycle state machines
- attach/detach tracking
- cleanup orchestration
- helper abstractions
- fallback paths
- deterministic tie-breakers
- duplicate validation
- synchronization
- caching
- exception wrapping

solely because they might theoretically be useful.

Use framework lifecycle and guarantees directly when they are sufficient.

Do not reimplement behavior already supplied by Unity, C#, UI Toolkit, or the surrounding architecture.

### Keep abstractions proportional

Do not create a new class, interface, wrapper, service, or helper merely to make code look architecturally complete.

A small direct implementation is preferred when it accurately represents the responsibility.

Extract something only when the extraction has a concrete semantic purpose, not merely to reduce visible code in a method.

### Respect semantic ownership

Two collections or values containing the same objects are not necessarily redundant if they represent different contracts or ownership.

Do not merge state solely because the current values happen to be identical.

Reason about what each piece of state means, not only about its runtime contents.

### Prefer transparent code

Prefer code whose behavior can be understood locally.

Avoid bookkeeping state whose only purpose is to coordinate other bookkeeping state.

Prefer:
```javascript
_screens.Sort(static (x, y) => x.order.CompareTo(y.order));
```

over adding unrelated tie-breakers for deterministic ordering.

### Changes must earn their complexity

Every nontrivial addition should answer:

> What concrete requirement or bug makes this necessary?

If there is no strong answer, leave the code simpler.

When reviewing existing code, do not "improve" something merely because a more defensive, deterministic, generalized, or extensible version can be imagined.

Preserve intentional simplicity.

### When uncertain

If something looks unusual but is internally valid, do not silently normalize it into a conventional pattern.

Preserve the existing behavior and mention the concern separately rather than changing the design.
