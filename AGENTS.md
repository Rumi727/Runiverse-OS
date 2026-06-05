# Agent Instructions

## Change Disclosure

- Do not make silent behavior changes while fixing or migrating code.
- If you notice and fix an issue outside the user's explicit request, call it out clearly in the response.
- This includes changes that look obviously correct, such as changing file-system operations to use a provider root (`targetPath + path`) instead of the raw path.
- Explain why the change was made, what behavior it changes, and that it can be reverted if the previous behavior was intentional.

## Language

- Unless the user explicitly asks for another language, respond in Korean by default.
- Even if the user writes in English, respond in Korean by default unless the user explicitly asks to change languages.

## Project Context

- This project is a Unity/C# project.
- When the user asks a question that requires understanding this project's structure, do not guess the structure from imagination. Inspect the actual files first, usually under the `/Packages` folder.

## C# Style

- Put `#nullable enable` at the very top of C# files.
