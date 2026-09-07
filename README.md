# THIS PROJECT WAS MADE WITH AI, USE TO STUDY OR TRY IT YOURSELF

# QuillLang
Quill, the scripting language (Made using AI)

Made using GLM-5.2, to compile you need .NET 8 SDK

```
dotnet build Quill.csproj
```

```
Quill/
├── Quill.csproj
├── Program.cs
├── Lexer.cs
├── Token.cs
├── Ast.cs
├── Parser.cs
├── Interpreter.cs
└── examples.qll
```

Example

``` quill

// Quill example program

let name = "World";
let count = 5;

print("Hello, " + name + "!");

// Variables and arithmetic
let pi = 3.14159;
let radius = 10;
let area = pi * radius * radius;
print("Circle area: " + string(area));

// For loop
for (let i = 1; i <= count; i = i + 1) {
    print("Iteration " + string(i));
}

// Recursive functions
func factorial(n) {
    if (n <= 1) { return 1; }
    return n * factorial(n - 1);
}

func fib(n) {
    if (n < 2) { return n; }
    return fib(n - 1) + fib(n - 2);
}

print("5! = " + string(factorial(5)));
print("fib(10) = " + string(fib(10)));

// While + break + continue
let i = 0;
while (true) {
    i = i + 1;
    if (i % 2 == 0) { continue; }
    if (i > 10) { break; }
    print("Odd: " + string(i));
}

// Built-ins
print("Length of 'Quill': " + string(len("Quill")));
print("Square root of 144: " + string(sqrt(144)));
print("Random dice roll: " + string(random(6) + 1));

// User input
print("What is your name?");
let who = input();
print("Hi, " + who + "!");

// Higher-order-style usage: pass function names around
func apply(fn, x) { return fn(x); }
func double(n) { return n * 2; }
print("Doubled 21 = " + string(apply(double, 21)));

```
