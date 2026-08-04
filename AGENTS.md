# Agent Instructions

## Change Authorization

- Do not modify any file unless the user explicitly asks for a change.
- Requests to inspect, diagnose, explain, review, or identify a cause authorize read-only investigation only.
- Do not infer permission to implement a fix from a reported bug or problem.
- After a read-only investigation, describe the proposed change and wait for explicit user authorization before editing files.

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
