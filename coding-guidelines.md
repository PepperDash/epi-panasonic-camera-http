# C# Coding Guidelines

## Coding Conventions

As a general rule, our coding convention is to follow the style of the surrounding code.
So if a file happens to differ in style from conventions defined here
(e.g. private members are named `m_member` rather than `_member`),
the existing style in that file takes precedence.

When making changes, you may find some existing code goes against the conventions defined here.
In such cases, please avoid reformatting any existing code when submitting a PR as it obscures the functional changes of the PR.
A separate PR should be submitted for style-only changes.
We also run the [.NET code formatter tool](https://github.com/dotnet/codeformatter) regularly to keep consistent formatting.

### Naming Conventions

- Use meaningful, descriptive words for names.
  For method names, it's encouraged to use `VerbObject` pair such as **`LoadModule`**.

- Use `_camelCase` to name internal and private fields and use `readonly` where possible.
  Prefix instance fields with `_`, static fields with `s_` and thread static fields with `t_`.
  When used on static fields, `readonly` should come after `static` (i.e. `static readonly` not `readonly static`).

- Use `camelCase` to name non-constant local variables.

- Use `PascalCase` to name constant local variables and fields.
  The only exception is for interop code where the constant should exactly match the name and value of the code you are calling via interop (e.g. `const int ERROR_SUCCESS = 0`).

- Use `PascalCase` to name types and all other type members.

### Layout Conventions

- Use four spaces of indentation (no tabs).

- Avoid more than one blank empty line at any time.

- Avoid trailing spaces at the end of a line.

- Braces usually go on their own lines,
  with the exception of single line statements that are properly indented.

- Namespace imports should be specified at the top of the file,
  outside of `namespace` declarations.

- Fields should be specified at the top within type declarations.
  For those that serve as backing fields for properties,
  they should be specified next to the corresponding properties.

- Preprocessor directives like `#if` and `#endif` should be placed at the beginning of a line,
  without any leading spaces.

- File encoding should be `ASCII`.
  All `BOM` encodings should be avoided.
  Tests that need a `BOM` encoding file should generate the file on the fly.

### Member Conventions

- Use of `this` is neither encouraged nor discouraged.

- Always specify the visibility, even if it's the default (i.e. `private string _foo` not `string _foo`).
  Visibility should be the first modifier (i.e. `public abstract` not `abstract public`).

- Make members private where possible.
  Avoid declaring public members unless it's absolutely necessary.

### Commenting Conventions

- Place the comment on a separate line, not at the end of a line of code.

- Begin comment text with an uppercase letter.
  It's recommended to end comment text with a period but not required.

- Add comments where the code is not trivial or could be confusing.

- Add comments where a reviewer needs help to understand the code.

- Update/remove existing comments when you are changing the corresponding code.

- Make sure the added/updated comments are meaningful, accurate and easy to understand.

- Public members must use [doc comments](https://docs.microsoft.com/dotnet/csharp/programming-guide/xmldoc/xml-documentation-comments).
  Internal and private members may use doc comments but it is not required.

## Performance Considerations

Essentials has a lot of performance sensitive code as well as a lot of inefficient code.
We have some guidelines that we typically apply widely even in less important code because code and patterns are copied,
and we want certain inefficient code to stay out of the performance critical code.

Some general guidelines:

- Avoid LINQ - it can create lots of avoidable garbage.
  Instead, iterate through a collection directly using `for` or `foreach` loop.

- Between `for` and `foreach`,
  `for` is slightly preferred when you're uncertain if `foreach` allocates an iterator.

- Avoid `params` arrays, prefer adding overloads with 1, 2, 3, and maybe more parameters.

- Be aware of APIs such as `String.Split(params char[])` that do not provide overloads to avoid array allocation.
  When calling such APIs, reuse a static array when possible.

- Avoid using string interpolations and overloads with implicit parameters such as `Culture` and `StringComparison`.
  Instead, use overloads with more explicit parameters such as `String.Format(IFormatProvider, String, Object[])` and `Equals(String, String, StringComparison)`.

- Avoid unnecessary memory allocation in a loop.
  Move the memory allocation outside the loop if possible.

- Avoid gratuitous exceptions as much as possible.
  Exception handling can be expensive due to cache misses and page faults when accessing the handling code and data.
  Finding and designing away exception-heavy code can result in a decent performance win.
  For example, you should stay away from things like using exceptions for control flow.

- Avoid `if (obj is Example) { example = (Example)obj }` when casting an object to a type.
  Instead, use `var example = obj as Example`.
  In this way you can avoid converting to the type twice.

- Use generic collections instead of the non-generic ones such as `ArrayList` and `Hashtable` to avoid type casting and unnecessary boxing whenever possible.

- Use collection constructor overloads that take an initial capacity for collection types that have them.
  Internally, `List<T>`, `Dictionary<TKey, TValue>`,
  and the other generic collections use one or more arrays to hold valid data.
  Whenever resizing is needed,
  one or more new arrays double the size of existing arrays are created and items from the existing arrays are copied.
  Setting an approximate initial capacity will reduce the number of resizing operations.

- Use `dict.TryGetValue` instead of `dict.Contains` and `dict[..]` when retrieving value from a `Dictionary`.
  In this way you can avoid hashing the key twice.

- It's OK to use the `+` operator to concatenate one-off short strings.
  But when dealing with strings in loops or large amounts of text,
  use a `StringBuilder` object.

## Best Practices

- Avoid hard-coding anything unless it's absolutely necessary.

- Avoid a method that is too long and complex.
  In such case, separate it to multiple methods or even a nested class as you see fit.

- Use the `using` statement instead of `try/finally` if the only code in the `finally` block is to call the `Dispose` method.

- Use of object initializers (e.g. `new Example { Name = "Name", ID = 1 }`) is encouraged for better readability,
  but not required.

- Stick to the `DRY` principle -- Don't Repeat Yourself.

  - Wrap the commonly used code in methods,
    or even put it in a utility class if that makes sense,
    so that the same code can be reused (e.g. `StringToBase64Converter.Base64ToString(string)`).
  - Check if the code for the same purpose already exists in the code base before inventing your own wheel.
  - Avoid repeating literal strings in code. Instead, use `const` variable to hold the string.
  - Resource strings used for errors or UI should be put in resource files (`.resx`) so that they can be localized later.

- Use of new C# language syntax is encouraged.
  But avoid refactoring any existing code using new language syntax when submitting a PR
  as it obscures the functional changes of the PR.
  A separate PR should be submitted for such refactoring without any functional changes.

- Consider using the `Interlocked` class instead of the `lock` statement to atomically change simple states. The `Interlocked` class provides better performance for updates that must be atomic.

- Here are some useful links for your reference:
  - [Framework Design Guidelines](https://docs.microsoft.com/dotnet/standard/design-guidelines/index) - Naming, Design and Usage guidelines including:
    - [Arrays](https://docs.microsoft.com/dotnet/standard/design-guidelines/arrays)
    - [Collections](https://docs.microsoft.com/dotnet/standard/design-guidelines/guidelines-for-collections)
    - [Exceptions](https://docs.microsoft.com/dotnet/standard/design-guidelines/exceptions)
  - [Best Practices for Developing World-Ready Applications](https://docs.microsoft.com/dotnet/standard/globalization-localization/best-practices-for-developing-world-ready-apps) - Unicode, Culture, Encoding and Localization.
  - [Best Practices for Exceptions](https://docs.microsoft.com/dotnet/standard/exceptions/best-practices-for-exceptions)
  - [Best Practices for Using Strings in .NET](https://docs.microsoft.com/dotnet/standard/base-types/best-practices-strings)
  - [Best Practices for Regular Expressions in .NET](https://docs.microsoft.com/dotnet/standard/base-types/best-practices)
  - [Serialization Guidelines](https://docs.microsoft.com/dotnet/standard/serialization/serialization-guidelines)
  - [Managed Threading Best Practices](https://docs.microsoft.com/dotnet/standard/threading/managed-threading-best-practices)
