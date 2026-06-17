# Text System Overview

Language available: \[[한국어 (대한민국)](README.md)\] \[[**English (US)**](README-EN.md)\]  

## Overview

This project's text system does not build a `string` immediately.\
It first creates a structured `Text` tree, then converts that tree into Unity/TMP rich text at render time.

```text
Text tree
-> RichTextBuilder
-> TMP rich text string
```

For simple output, directly writing a rich text string is usually much faster and simpler.

```csharp
$"<color=yellow>HP: {hp:0.##}</color>"
```

The purpose of the `Text` system is not raw string creation speed.\
Its main benefit is that text can be kept as structured data, passed through APIs, combined, localized, styled, and rendered later.

## Why Text Tree

A `string` is already the final result.\
After text is concatenated, it is hard to know which part was a value, which part had style, or which value had formatting.

`Text` preserves that information until rendering.

```text
GroupText
 |- LiteralText("HP: ")
 `- LiteralText(100, format: "0.##").Yellow()
```

This makes text behave like meaningful pieces of data, not just output characters.

## Core Types

`Text` is the base type for all text elements.\
Style is also attached to `Text`.

```csharp
Text.Literal("Warning").Bold().Red();
```

`LiteralText` stores an actual value.\
It can store strings, numbers, dates, formatting, and alignment.

```csharp
Text.Literal(123, 5, "000");
```

`GroupText` stores multiple `Text` elements in order.

```csharp
GroupText text = $"HP: {100:0.##}";
```

`LocalizationText` stores a localization key and `Text` arguments.\
Formatted positions such as `{0}` and `{1}` can contain styled text.

## Reusable Factory Assumption

Static factory methods such as `Text.Literal`, `Text.Local`, and `Text.Group` are assumed to reuse internal instances.

Normal user code should prefer factory methods over directly creating text objects.

```csharp
Text text = Text.Literal("HP");
```

This reduces object allocation when the same kind of text is created frequently.

## Structured Text as API Data

Because `Text` is not yet a rendered string, it can be passed around as a parameter.

```csharp
void SetTitle(Text title)
{
    string richText = RichTextBuilder.Build(title);
}
```

Callers can pass plain text, styled text, localized text, or grouped text through the same API.

```csharp
SetTitle(Text.Literal("Loading").Yellow());
SetTitle(Text.Local("ui.loading.title"));
SetTitle(Text.Group($"HP: {hp:0.##}"));
```

The receiver does not need to know whether the text came from a literal value, localization, or a composed group.\
It only needs to render the `Text` when necessary.

## Mutable Text Instances

`Text` is not just a value object that is created once and forgotten.\
It is a mutable instance whose value can be changed.

For example, one system can receive a `Text`, store it, and only render it later.

```csharp
LiteralText progressText = new LiteralText(0, "0.##");
Text description = Text.Group($"Progress: {progressText}%");

SetDescription(description);
```

Later, code outside the receiver can update the same `Text` instance.

```csharp
progressText.value = 50;
```

The renderer still renders the same `description`, but the rendered result uses the updated value.

```text
Progress: 50%
```

This is especially useful for text where only the value changes in real time, such as progress, a current file path, or a status message.\
The text structure stays the same while the inner value changes.

## Interpolated Strings

`GroupTextStringHandler` allows C# interpolated strings to create `GroupText`.

```csharp
Text name = Text.Literal("Rumi").Bold();
GroupText text = $"Player {name}: {100:000}";
```

The structure is roughly:

```text
GroupText
 |- LiteralText("Player ")
 |- Text.Literal("Rumi").Bold()
 |- LiteralText(": ")
 `- LiteralText(100, format: "000")
```

If an interpolation value is already `Text`, it is inserted as-is and keeps its style.\
Normal values become `LiteralText`, and their format/alignment information is preserved.

## Rendering Flow

Rendering starts with `RichTextBuilder.Build(text)`.

```csharp
string richText = RichTextBuilder.Build(text);
```

The builder finds a renderer based on the runtime type of the `Text`.

```text
LiteralText      -> LiteralRichTextBuilder
GroupText        -> GroupRichTextBuilder
LocalizationText -> LocalizationRichTextBuilder
```

`GroupText` renders its children in order.\
`LocalizationText` resolves a localization format and renders `Text` arguments into the formatted positions.

## Extensibility

The text system is not hardcoded to handle only built-in text types.\
You can add a new `Text` type and a builder that renders it.

A builder uses `CustomTextRendererAttribute` to declare which `Text` type it handles.

```csharp
[CustomTextRenderer(typeof(MyText))]
public sealed class MyRichTextBuilder : RichTextBuilder
{
    protected override void AppendCore(StringBuilder builder, Text text, TextStyleState styleState)
    {
        MyText myText = (MyText)text;
        // render myText
    }
}
```

`RichTextBuilder` finds the registered builder from the runtime type of the `Text`.\
This means the text structure can be extended when needed.

TMP rich text tag output is the exception.\
TMP has a fixed set of supported tags, so that part is hardcoded in `RichTextUtility` for rendering performance.

## Style Stack

Styles can be nested.

```text
Red
 `- Bold
    `- "Hello"
```

During rendering, `TextStyleState` tracks the current style stack.

```text
Open Red
  Open Bold
    Append "Hello"
  Close Bold
Close Red
```

The result is TMP rich text.

```text
<color=red><b>Hello</b></color>
```

After a child has rendered, the renderer must return to the parent style state.\
If this restore step is wrong, styles can leak into following text.

## GC and Performance

Direct rich text strings are usually faster.

```csharp
$"<color=yellow>HP: {hp:0.##}</color>"
```

The `Text` system has extra work:

```text
Create or reuse Text structure
Track style state
Build rich text string
Update TMP layout
```

Reusable factory methods reduce GC from `Text` object creation.\
Rendering also reduces temporary allocation through `StringBuilderCache`, `TextStyleStateCache`, and builder caching.

However, the final rich text `string` still has to be produced for TMP when the value changes.\
TMP layout cost can also happen when the displayed string changes.

For frequently updated UI, build only when the value changes.

```csharp
if (hp != previousHp)
{
    previousHp = hp;
    label.text = RichTextBuilder.Build(Text.Group($"HP: {hp:0.##}"));
}
```

## Summary

If only fast output is needed, direct rich text strings are better.\
The `Text` system exists for structure.

`Text` preserves values, styles, localization, formatting, and grouping until render time.\
That makes text easy to pass as parameters, combine, reuse, and render later.
