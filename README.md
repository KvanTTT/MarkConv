# MarkConv (Markdown Converter)

Converts markdown of different types (GitHub, Habr, Dev) to each other.
GitHub is also equivalent to VisualCode.

Platform: NET Core 3.1 (Crossplatform).

## Using

The following command:

```
dotnet MarkConv.Cli.dll -f "MyAwesomeArticle.md" -o Habr
```

Creates the output file `MyAwesomeArticle-Common-Habr.md`.

All cli parameters are optional except of `-f`.

### `-f`

File to convert.

### `-i` or `-o`

Supported values:

* GitHub (Default)
* Habr
* Dev

### `-l` or `--lineslength`

Max length of line. `0` - not change, `-1` - merge lines

### `--removetitleheader`

Removes title header.

## Build Status

| Windows & Linux Build Status |
|---|
| [![Build status](https://ci.appveyor.com/api/projects/status/jc9rqhgf7k8h5ajc?svg=true)](https://ci.appveyor.com/project/KvanTTT/markconv) |