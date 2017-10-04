# Test reproducing request timeout

Simply build the project and execute the tests.

```
$ dotnet restore

$ dotnet build

$ dotnet test
```

The test is green on Windows, but fails (the server returns a `RequestTimeout`) on Linux.