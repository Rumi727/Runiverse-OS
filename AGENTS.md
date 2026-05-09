# Agent Instructions

## Change Disclosure

- Do not make silent behavior changes while fixing or migrating code.
- If you notice and fix an issue outside the user's explicit request, call it out clearly in the response.
- This includes changes that look obviously correct, such as changing file-system operations to use a provider root (`targetPath + path`) instead of the raw path.
- Explain why the change was made, what behavior it changes, and that it can be reverted if the previous behavior was intentional.
- Before modifying code, explain the intended change and get the user's review/approval unless the user has already explicitly authorized the edit.

## Language

- Unless the user explicitly asks for another language, respond in Korean by default.

## C# Style

- Put `#nullable enable` at the very top of C# files.
