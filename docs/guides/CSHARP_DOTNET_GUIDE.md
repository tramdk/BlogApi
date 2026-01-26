# 📘 C# & .NET Core - Hướng dẫn Cơ Bản & Cốt Lõi

**Ngôn ngữ**: C# (C-Sharp)  
**Platform**: .NET Core / .NET 6+  
**Mục đích**: Tổng hợp kiến thức cơ bản và cốt lõi cần thiết

---

## 📑 Mục lục

1. [C# Basics](#-c-basics)
2. [Types & Variables](#-types--variables)
3. [Collections](#-collections)
4. [LINQ](#-linq)
5. [Async/Await](#-asyncawait)
6. [Dependency Injection](#-dependency-injection)
7. [Generics](#-generics)
8. [Delegates & Events](#-delegates--events)
9. [Exception Handling](#-exception-handling)
10. [File I/O](#-file-io)
11. [Attributes](#-attributes)
12. [Reflection](#-reflection)
13. [Extension Methods](#-extension-methods)
14. [Pattern Matching](#-pattern-matching)
15. [Records](#-records)
16. [Nullable Reference Types](#-nullable-reference-types)
17. [Best Practices](#-best-practices)

---

## 🎯 C# Basics

### 1. Program Structure

```csharp
// Top-level statements (C# 9+)
using System;

Console.WriteLine("Hello, World!");

// Traditional structure
namespace MyApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }
}
```

### 2. Namespaces

```csharp
// Declaring namespace
namespace MyCompany.MyProject.Features
{
    public class MyClass { }
}

// Using namespaces
using System;
using System.Collections.Generic;
using System.Linq;

// Using alias
using Project = MyCompany.MyProject;

// Global using (C# 10+)
global using System;
global using System.Collections.Generic;
```

### 3. Classes & Objects

```csharp
// Class definition
public class Person
{
    // Fields (private by convention)
    private string _name;
    private int _age;
    
    // Properties (public)
    public string Name 
    { 
        get => _name; 
        set => _name = value; 
    }
    
    // Auto-property
    public int Age { get; set; }
    
    // Read-only property
    public string FullName { get; }
    
    // Constructor
    public Person(string name, int age)
    {
        Name = name;
        Age = age;
        FullName = $"{name} ({age})";
    }
    
    // Method
    public void SayHello()
    {
        Console.WriteLine($"Hello, I'm {Name}");
    }
    
    // Static method
    public static Person CreateDefault()
    {
        return new Person("Unknown", 0);
    }
}

// Object creation
var person = new Person("John", 30);
person.SayHello();
```

### 4. Inheritance

```csharp
// Base class
public class Animal
{
    public string Name { get; set; }
    
    public virtual void MakeSound()
    {
        Console.WriteLine("Some sound");
    }
}

// Derived class
public class Dog : Animal
{
    public override void MakeSound()
    {
        Console.WriteLine("Woof!");
    }
    
    public void Fetch()
    {
        Console.WriteLine($"{Name} is fetching");
    }
}

// Usage
Animal animal = new Dog { Name = "Buddy" };
animal.MakeSound(); // Output: Woof!
```

### 5. Interfaces

```csharp
// Interface definition
public interface IRepository<T>
{
    Task<T> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

// Implementation
public class PostRepository : IRepository<Post>
{
    public async Task<Post> GetByIdAsync(Guid id)
    {
        // Implementation
    }
    
    // ... other methods
}

// Multiple interfaces
public class MyClass : IDisposable, IComparable<MyClass>
{
    public void Dispose() { }
    public int CompareTo(MyClass other) => 0;
}
```

---

## 📦 Types & Variables

### 1. Value Types vs Reference Types

```csharp
// VALUE TYPES (stored on stack)
int number = 42;                    // System.Int32
double price = 19.99;               // System.Double
bool isActive = true;               // System.Boolean
char letter = 'A';                  // System.Char
DateTime now = DateTime.Now;        // System.DateTime
Guid id = Guid.NewGuid();          // System.Guid

// Struct (value type)
public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
}

// REFERENCE TYPES (stored on heap)
string name = "John";               // System.String
object obj = new object();          // System.Object
int[] numbers = new int[5];         // Array
List<int> list = new List<int>();   // Collection

// Class (reference type)
public class Person
{
    public string Name { get; set; }
}
```

### 2. Nullable Types

```csharp
// Nullable value types
int? nullableInt = null;
DateTime? nullableDate = null;

// Check for null
if (nullableInt.HasValue)
{
    Console.WriteLine(nullableInt.Value);
}

// Null-coalescing operator
int value = nullableInt ?? 0; // Use 0 if null

// Null-conditional operator
string? name = null;
int? length = name?.Length; // null if name is null
```

### 3. String Operations

```csharp
// String creation
string str1 = "Hello";
string str2 = "World";

// Concatenation
string result = str1 + " " + str2;

// String interpolation (preferred)
string message = $"{str1} {str2}!";

// Verbatim string
string path = @"C:\Users\Documents\file.txt";

// Multi-line string
string multiLine = @"Line 1
Line 2
Line 3";

// String methods
bool contains = message.Contains("Hello");
string upper = message.ToUpper();
string lower = message.ToLower();
string trimmed = message.Trim();
string replaced = message.Replace("Hello", "Hi");
string[] parts = message.Split(' ');

// StringBuilder (for multiple concatenations)
var sb = new StringBuilder();
for (int i = 0; i < 1000; i++)
{
    sb.Append(i);
}
string result = sb.ToString();
```

### 4. Type Conversion

```csharp
// Implicit conversion (safe)
int num = 42;
long bigNum = num; // int → long

// Explicit conversion (cast)
double d = 3.14;
int i = (int)d; // 3 (truncates)

// Parse
string numStr = "123";
int parsed = int.Parse(numStr);

// TryParse (safer)
if (int.TryParse(numStr, out int result))
{
    Console.WriteLine(result);
}

// Convert class
string str = "123";
int num = Convert.ToInt32(str);
bool b = Convert.ToBoolean("true");

// ToString
int number = 42;
string text = number.ToString();
```

---

## 📚 Collections

### 1. Arrays

```csharp
// Array declaration
int[] numbers = new int[5];
int[] numbers2 = { 1, 2, 3, 4, 5 };
int[] numbers3 = new int[] { 1, 2, 3 };

// Multi-dimensional array
int[,] matrix = new int[3, 3];
matrix[0, 0] = 1;

// Jagged array
int[][] jagged = new int[3][];
jagged[0] = new int[] { 1, 2 };
jagged[1] = new int[] { 3, 4, 5 };

// Array operations
int length = numbers.Length;
Array.Sort(numbers);
Array.Reverse(numbers);
int index = Array.IndexOf(numbers, 3);
```

### 2. List<T>

```csharp
// List creation
var numbers = new List<int>();
var names = new List<string> { "John", "Jane", "Bob" };

// Add items
numbers.Add(1);
numbers.AddRange(new[] { 2, 3, 4 });

// Access items
int first = numbers[0];
int last = numbers[^1]; // C# 8+ (index from end)

// Remove items
numbers.Remove(1);      // Remove first occurrence
numbers.RemoveAt(0);    // Remove at index
numbers.Clear();        // Remove all

// Search
bool contains = names.Contains("John");
int index = names.IndexOf("Jane");
string found = names.Find(n => n.StartsWith("J"));
List<string> filtered = names.FindAll(n => n.Length > 3);

// Iterate
foreach (var name in names)
{
    Console.WriteLine(name);
}

// LINQ methods
var sorted = names.OrderBy(n => n).ToList();
var filtered = names.Where(n => n.StartsWith("J")).ToList();
```

### 3. Dictionary<TKey, TValue>

```csharp
// Dictionary creation
var ages = new Dictionary<string, int>();
var scores = new Dictionary<string, int>
{
    { "John", 95 },
    { "Jane", 87 },
    { "Bob", 92 }
};

// Add items
ages.Add("John", 30);
ages["Jane"] = 25; // Add or update

// Access items
int johnAge = ages["John"];

// Safe access
if (ages.TryGetValue("John", out int age))
{
    Console.WriteLine(age);
}

// Check existence
bool hasJohn = ages.ContainsKey("John");
bool hasAge30 = ages.ContainsValue(30);

// Remove
ages.Remove("John");

// Iterate
foreach (var kvp in ages)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

// Keys and Values
var keys = ages.Keys;
var values = ages.Values;
```

### 4. HashSet<T>

```csharp
// HashSet (unique items, fast lookup)
var uniqueNumbers = new HashSet<int> { 1, 2, 3, 4, 5 };

// Add
uniqueNumbers.Add(6);
uniqueNumbers.Add(1); // Ignored (already exists)

// Set operations
var set1 = new HashSet<int> { 1, 2, 3 };
var set2 = new HashSet<int> { 3, 4, 5 };

set1.UnionWith(set2);        // { 1, 2, 3, 4, 5 }
set1.IntersectWith(set2);    // { 3 }
set1.ExceptWith(set2);       // { 1, 2 }
```

### 5. Queue<T> & Stack<T>

```csharp
// Queue (FIFO - First In First Out)
var queue = new Queue<string>();
queue.Enqueue("First");
queue.Enqueue("Second");
queue.Enqueue("Third");

string first = queue.Dequeue(); // "First"
string peek = queue.Peek();     // "Second" (doesn't remove)

// Stack (LIFO - Last In First Out)
var stack = new Stack<string>();
stack.Push("First");
stack.Push("Second");
stack.Push("Third");

string last = stack.Pop();      // "Third"
string peek = stack.Peek();     // "Second" (doesn't remove)
```

---

## 🔍 LINQ

### 1. LINQ Basics

```csharp
var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

// Query syntax
var evenNumbers = from n in numbers
                  where n % 2 == 0
                  select n;

// Method syntax (preferred)
var evenNumbers2 = numbers.Where(n => n % 2 == 0);

// Both return IEnumerable<int>
```

### 2. Filtering

```csharp
var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

// Where
var evenNumbers = numbers.Where(n => n % 2 == 0);
var greaterThan5 = numbers.Where(n => n > 5);

// Multiple conditions
var filtered = numbers.Where(n => n > 3 && n < 8);

// First, FirstOrDefault
int first = numbers.First(n => n > 5);           // 6 (throws if not found)
int firstOrDefault = numbers.FirstOrDefault(n => n > 100); // 0 (default)

// Single, SingleOrDefault
int single = numbers.Single(n => n == 5);        // 5 (throws if 0 or >1)
int singleOrDefault = numbers.SingleOrDefault(n => n == 100); // 0

// Last, LastOrDefault
int last = numbers.Last(n => n < 5);             // 4
```

### 3. Projection

```csharp
var people = new List<Person>
{
    new Person { Name = "John", Age = 30 },
    new Person { Name = "Jane", Age = 25 },
    new Person { Name = "Bob", Age = 35 }
};

// Select (transform)
var names = people.Select(p => p.Name);
var ages = people.Select(p => p.Age);

// Select with anonymous type
var projected = people.Select(p => new 
{ 
    p.Name, 
    IsAdult = p.Age >= 18 
});

// SelectMany (flatten)
var orders = new List<Order>
{
    new Order { Items = new[] { "A", "B" } },
    new Order { Items = new[] { "C", "D" } }
};

var allItems = orders.SelectMany(o => o.Items);
// Result: ["A", "B", "C", "D"]
```

### 4. Ordering

```csharp
var people = GetPeople();

// OrderBy (ascending)
var sorted = people.OrderBy(p => p.Age);

// OrderByDescending
var sortedDesc = people.OrderByDescending(p => p.Age);

// ThenBy (secondary sort)
var sorted2 = people
    .OrderBy(p => p.Age)
    .ThenBy(p => p.Name);

// Reverse
var reversed = people.Reverse();
```

### 5. Grouping

```csharp
var people = GetPeople();

// GroupBy
var grouped = people.GroupBy(p => p.Age);

foreach (var group in grouped)
{
    Console.WriteLine($"Age {group.Key}:");
    foreach (var person in group)
    {
        Console.WriteLine($"  {person.Name}");
    }
}

// GroupBy with projection
var ageGroups = people
    .GroupBy(p => p.Age)
    .Select(g => new 
    { 
        Age = g.Key, 
        Count = g.Count(),
        Names = g.Select(p => p.Name).ToList()
    });
```

### 6. Aggregation

```csharp
var numbers = new List<int> { 1, 2, 3, 4, 5 };

// Count
int count = numbers.Count();
int evenCount = numbers.Count(n => n % 2 == 0);

// Sum
int sum = numbers.Sum();
int sumOfSquares = numbers.Sum(n => n * n);

// Average
double avg = numbers.Average();

// Min, Max
int min = numbers.Min();
int max = numbers.Max();

// Aggregate (custom)
int product = numbers.Aggregate((a, b) => a * b); // 1*2*3*4*5 = 120
```

### 7. Set Operations

```csharp
var list1 = new List<int> { 1, 2, 3, 4, 5 };
var list2 = new List<int> { 4, 5, 6, 7, 8 };

// Distinct
var unique = list1.Concat(list2).Distinct();

// Union (unique items from both)
var union = list1.Union(list2); // { 1, 2, 3, 4, 5, 6, 7, 8 }

// Intersect (common items)
var intersect = list1.Intersect(list2); // { 4, 5 }

// Except (items in first but not in second)
var except = list1.Except(list2); // { 1, 2, 3 }
```

### 8. Joining

```csharp
var customers = new List<Customer>
{
    new Customer { Id = 1, Name = "John" },
    new Customer { Id = 2, Name = "Jane" }
};

var orders = new List<Order>
{
    new Order { CustomerId = 1, Product = "Book" },
    new Order { CustomerId = 1, Product = "Pen" },
    new Order { CustomerId = 2, Product = "Laptop" }
};

// Join
var result = customers.Join(
    orders,
    c => c.Id,
    o => o.CustomerId,
    (c, o) => new { c.Name, o.Product }
);

// GroupJoin (left join)
var result2 = customers.GroupJoin(
    orders,
    c => c.Id,
    o => o.CustomerId,
    (c, orderGroup) => new 
    { 
        c.Name, 
        Orders = orderGroup.ToList() 
    }
);
```

### 9. Quantifiers

```csharp
var numbers = new List<int> { 1, 2, 3, 4, 5 };

// Any (at least one)
bool hasEven = numbers.Any(n => n % 2 == 0); // true

// All (every item)
bool allPositive = numbers.All(n => n > 0); // true

// Contains
bool hasThree = numbers.Contains(3); // true
```

### 10. Deferred Execution

```csharp
var numbers = new List<int> { 1, 2, 3, 4, 5 };

// Deferred execution (query not executed yet)
var query = numbers.Where(n => n > 3);

// Add more items
numbers.Add(6);
numbers.Add(7);

// Execute now (includes 6 and 7)
var result = query.ToList(); // { 4, 5, 6, 7 }

// Immediate execution
var immediate = numbers.Where(n => n > 3).ToList();
numbers.Add(8); // Not included in 'immediate'
```

---

## ⚡ Async/Await

### 1. Async Basics

```csharp
// Async method signature
public async Task<string> GetDataAsync()
{
    // Simulate async operation
    await Task.Delay(1000);
    return "Data";
}

// Async void (only for event handlers)
private async void Button_Click(object sender, EventArgs e)
{
    await DoSomethingAsync();
}

// Calling async methods
public async Task ProcessAsync()
{
    // Await the result
    string data = await GetDataAsync();
    Console.WriteLine(data);
}
```

### 2. Task vs Task<T>

```csharp
// Task (no return value)
public async Task DoWorkAsync()
{
    await Task.Delay(1000);
    Console.WriteLine("Work done");
}

// Task<T> (returns T)
public async Task<int> CalculateAsync()
{
    await Task.Delay(1000);
    return 42;
}

// Usage
await DoWorkAsync();
int result = await CalculateAsync();
```

### 3. Multiple Async Operations

```csharp
// Sequential (slow)
public async Task SequentialAsync()
{
    var result1 = await GetData1Async(); // Wait
    var result2 = await GetData2Async(); // Wait
    var result3 = await GetData3Async(); // Wait
}

// Concurrent (fast)
public async Task ConcurrentAsync()
{
    var task1 = GetData1Async(); // Start
    var task2 = GetData2Async(); // Start
    var task3 = GetData3Async(); // Start
    
    // Wait for all
    await Task.WhenAll(task1, task2, task3);
    
    var result1 = task1.Result;
    var result2 = task2.Result;
    var result3 = task3.Result;
}

// WhenAny (first to complete)
public async Task<string> GetFirstAsync()
{
    var task1 = GetData1Async();
    var task2 = GetData2Async();
    
    var completedTask = await Task.WhenAny(task1, task2);
    return await completedTask;
}
```

### 4. ConfigureAwait

```csharp
// In library code (don't capture context)
public async Task<string> GetDataAsync()
{
    var response = await httpClient.GetAsync(url)
        .ConfigureAwait(false);
    
    return await response.Content.ReadAsStringAsync()
        .ConfigureAwait(false);
}

// In UI code (capture context)
private async void Button_Click(object sender, EventArgs e)
{
    var data = await GetDataAsync(); // Captures UI context
    textBox.Text = data; // Can update UI
}
```

### 5. CancellationToken

```csharp
// Method with cancellation support
public async Task<string> GetDataAsync(CancellationToken ct)
{
    for (int i = 0; i < 10; i++)
    {
        // Check if cancellation requested
        ct.ThrowIfCancellationRequested();
        
        await Task.Delay(1000, ct);
    }
    
    return "Data";
}

// Usage
var cts = new CancellationTokenSource();

// Start task
var task = GetDataAsync(cts.Token);

// Cancel after 3 seconds
cts.CancelAfter(TimeSpan.FromSeconds(3));

try
{
    await task;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation cancelled");
}
```

### 6. Async Best Practices

```csharp
// ✅ DO: Use async all the way
public async Task<List<Post>> GetPostsAsync()
{
    return await _repository.GetAllAsync();
}

// ❌ DON'T: Block on async code
public List<Post> GetPosts()
{
    return GetPostsAsync().Result; // DEADLOCK!
}

// ✅ DO: Use ConfigureAwait(false) in libraries
public async Task<string> GetDataAsync()
{
    return await httpClient.GetStringAsync(url)
        .ConfigureAwait(false);
}

// ✅ DO: Return Task directly when possible
public Task<string> GetDataAsync()
{
    return httpClient.GetStringAsync(url);
}

// ❌ DON'T: Use async void (except event handlers)
public async void ProcessData() // BAD!
{
    await DoSomethingAsync();
}
```

---

## 💉 Dependency Injection

### 1. DI Basics

```csharp
// Service interface
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

// Service implementation
public class EmailService : IEmailService
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Send email logic
    }
}

// Registration (Program.cs)
builder.Services.AddScoped<IEmailService, EmailService>();

// Usage (constructor injection)
public class UserController : ControllerBase
{
    private readonly IEmailService _emailService;
    
    public UserController(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        // Use service
        await _emailService.SendEmailAsync(
            dto.Email, 
            "Welcome", 
            "Welcome to our app!");
        
        return Ok();
    }
}
```

### 2. Service Lifetimes

```csharp
// TRANSIENT: New instance every time
builder.Services.AddTransient<ITransientService, TransientService>();
// Use for: Lightweight, stateless services

// SCOPED: One instance per request
builder.Services.AddScoped<IScopedService, ScopedService>();
// Use for: DbContext, repositories, request-specific services

// SINGLETON: One instance for application lifetime
builder.Services.AddSingleton<ISingletonService, SingletonService>();
// Use for: Configuration, caching, logging

// Example
public class MyService
{
    private readonly ITransientService _transient;
    private readonly IScopedService _scoped;
    private readonly ISingletonService _singleton;
    
    public MyService(
        ITransientService transient,
        IScopedService scoped,
        ISingletonService singleton)
    {
        _transient = transient;
        _scoped = scoped;
        _singleton = singleton;
    }
}
```

### 3. Multiple Implementations

```csharp
// Multiple implementations
public interface INotificationService
{
    Task SendAsync(string message);
}

public class EmailNotificationService : INotificationService
{
    public async Task SendAsync(string message) { }
}

public class SmsNotificationService : INotificationService
{
    public async Task SendAsync(string message) { }
}

// Register all
builder.Services.AddScoped<INotificationService, EmailNotificationService>();
builder.Services.AddScoped<INotificationService, SmsNotificationService>();

// Inject all
public class NotificationManager
{
    private readonly IEnumerable<INotificationService> _services;
    
    public NotificationManager(IEnumerable<INotificationService> services)
    {
        _services = services;
    }
    
    public async Task SendToAllAsync(string message)
    {
        foreach (var service in _services)
        {
            await service.SendAsync(message);
        }
    }
}
```

### 4. Factory Pattern with DI

```csharp
// Factory interface
public interface IServiceFactory
{
    IService Create(string type);
}

// Factory implementation
public class ServiceFactory : IServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public ServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IService Create(string type)
    {
        return type switch
        {
            "email" => _serviceProvider.GetRequiredService<EmailService>(),
            "sms" => _serviceProvider.GetRequiredService<SmsService>(),
            _ => throw new ArgumentException("Invalid type")
        };
    }
}

// Registration
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddScoped<IServiceFactory, ServiceFactory>();
```

---

## 🔧 Generics

### 1. Generic Classes

```csharp
// Generic class
public class Repository<T> where T : class
{
    private readonly List<T> _items = new();
    
    public void Add(T item)
    {
        _items.Add(item);
    }
    
    public T GetById(int id)
    {
        return _items[id];
    }
    
    public List<T> GetAll()
    {
        return _items;
    }
}

// Usage
var postRepo = new Repository<Post>();
postRepo.Add(new Post { Title = "Hello" });

var userRepo = new Repository<User>();
userRepo.Add(new User { Name = "John" });
```

### 2. Generic Methods

```csharp
// Generic method
public T GetMax<T>(T a, T b) where T : IComparable<T>
{
    return a.CompareTo(b) > 0 ? a : b;
}

// Usage
int maxInt = GetMax(5, 10);
string maxStr = GetMax("apple", "banana");
```

### 3. Generic Constraints

```csharp
// Class constraint
public class Repository<T> where T : class
{
}

// Struct constraint
public class ValueRepository<T> where T : struct
{
}

// Interface constraint
public class SortedList<T> where T : IComparable<T>
{
}

// Base class constraint
public class AnimalRepository<T> where T : Animal
{
}

// Constructor constraint
public class Factory<T> where T : new()
{
    public T Create()
    {
        return new T();
    }
}

// Multiple constraints
public class MyClass<T> where T : class, IDisposable, new()
{
}
```

### 4. Generic Interfaces

```csharp
// Generic interface
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
}

// Implementation
public class PostRepository : IRepository<Post>
{
    public async Task<Post> GetByIdAsync(Guid id)
    {
        // Implementation
    }
    
    public async Task<List<Post>> GetAllAsync()
    {
        // Implementation
    }
    
    public async Task AddAsync(Post entity)
    {
        // Implementation
    }
}
```

---

## 🎪 Delegates & Events

### 1. Delegates

```csharp
// Delegate declaration
public delegate void MessageHandler(string message);
public delegate int Calculator(int a, int b);

// Usage
public class Messenger
{
    public void SendMessage(string message, MessageHandler handler)
    {
        handler(message);
    }
}

// Calling
var messenger = new Messenger();
messenger.SendMessage("Hello", msg => Console.WriteLine(msg));

// Built-in delegates
Action action = () => Console.WriteLine("Action");
Action<string> actionWithParam = msg => Console.WriteLine(msg);
Func<int> func = () => 42;
Func<int, int, int> add = (a, b) => a + b;
```

### 2. Events

```csharp
// Publisher
public class Button
{
    // Event declaration
    public event EventHandler Clicked;
    
    // Raise event
    public void Click()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}

// Subscriber
var button = new Button();
button.Clicked += (sender, e) => 
{
    Console.WriteLine("Button clicked!");
};

button.Click(); // Raises event
```

### 3. Custom EventArgs

```csharp
// Custom EventArgs
public class OrderEventArgs : EventArgs
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
}

// Publisher
public class OrderService
{
    public event EventHandler<OrderEventArgs> OrderPlaced;
    
    public void PlaceOrder(Guid orderId, decimal amount)
    {
        // Process order...
        
        // Raise event
        OrderPlaced?.Invoke(this, new OrderEventArgs 
        { 
            OrderId = orderId, 
            Amount = amount 
        });
    }
}

// Subscriber
var orderService = new OrderService();
orderService.OrderPlaced += (sender, e) =>
{
    Console.WriteLine($"Order {e.OrderId} placed for ${e.Amount}");
};
```

---

## ⚠️ Exception Handling

### 1. Try-Catch-Finally

```csharp
try
{
    // Code that might throw exception
    int result = 10 / 0;
}
catch (DivideByZeroException ex)
{
    // Handle specific exception
    Console.WriteLine($"Error: {ex.Message}");
}
catch (Exception ex)
{
    // Handle any other exception
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
finally
{
    // Always executed (cleanup)
    Console.WriteLine("Cleanup");
}
```

### 2. Custom Exceptions

```csharp
// Custom exception
public class ValidationException : Exception
{
    public List<string> Errors { get; }
    
    public ValidationException(string message, List<string> errors) 
        : base(message)
    {
        Errors = errors;
    }
}

// Usage
public void ValidateUser(User user)
{
    var errors = new List<string>();
    
    if (string.IsNullOrEmpty(user.Name))
        errors.Add("Name is required");
    
    if (user.Age < 18)
        errors.Add("Must be 18 or older");
    
    if (errors.Any())
        throw new ValidationException("Validation failed", errors);
}

// Catching
try
{
    ValidateUser(user);
}
catch (ValidationException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine(error);
    }
}
```

### 3. Exception Filters

```csharp
try
{
    // Code
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    // Handle 404
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
{
    // Handle 401
}
catch (Exception ex)
{
    // Handle others
}
```

### 4. Using Statement (IDisposable)

```csharp
// Using statement (automatic disposal)
using (var stream = File.OpenRead("file.txt"))
{
    // Use stream
} // stream.Dispose() called automatically

// Using declaration (C# 8+)
using var stream = File.OpenRead("file.txt");
// Use stream
// stream.Dispose() called at end of scope

// Multiple using
using var stream1 = File.OpenRead("file1.txt");
using var stream2 = File.OpenRead("file2.txt");
```

---

## 📁 File I/O

### 1. Reading Files

```csharp
// Read all text
string content = File.ReadAllText("file.txt");

// Read all lines
string[] lines = File.ReadAllLines("file.txt");

// Read all bytes
byte[] bytes = File.ReadAllBytes("file.bin");

// Read with StreamReader
using var reader = new StreamReader("file.txt");
string line;
while ((line = reader.ReadLine()) != null)
{
    Console.WriteLine(line);
}

// Async reading
string content = await File.ReadAllTextAsync("file.txt");
```

### 2. Writing Files

```csharp
// Write all text
File.WriteAllText("file.txt", "Hello, World!");

// Write all lines
string[] lines = { "Line 1", "Line 2", "Line 3" };
File.WriteAllLines("file.txt", lines);

// Append text
File.AppendAllText("file.txt", "New line\n");

// Write with StreamWriter
using var writer = new StreamWriter("file.txt");
writer.WriteLine("Line 1");
writer.WriteLine("Line 2");

// Async writing
await File.WriteAllTextAsync("file.txt", "Hello, World!");
```

### 3. File Operations

```csharp
// Check if file exists
bool exists = File.Exists("file.txt");

// Copy file
File.Copy("source.txt", "destination.txt");

// Move file
File.Move("old.txt", "new.txt");

// Delete file
File.Delete("file.txt");

// Get file info
var fileInfo = new FileInfo("file.txt");
long size = fileInfo.Length;
DateTime created = fileInfo.CreationTime;
DateTime modified = fileInfo.LastWriteTime;
```

### 4. Directory Operations

```csharp
// Create directory
Directory.CreateDirectory("MyFolder");

// Check if directory exists
bool exists = Directory.Exists("MyFolder");

// Get files in directory
string[] files = Directory.GetFiles("MyFolder");
string[] txtFiles = Directory.GetFiles("MyFolder", "*.txt");

// Get subdirectories
string[] dirs = Directory.GetDirectories("MyFolder");

// Delete directory
Directory.Delete("MyFolder", recursive: true);
```

---

## 🏷️ Attributes

### 1. Built-in Attributes

```csharp
// Obsolete
[Obsolete("Use NewMethod instead")]
public void OldMethod()
{
}

// Serializable
[Serializable]
public class Person
{
    public string Name { get; set; }
}

// Conditional
[Conditional("DEBUG")]
public void DebugLog(string message)
{
    Console.WriteLine(message);
}
```

### 2. Custom Attributes

```csharp
// Define custom attribute
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorAttribute : Attribute
{
    public string Name { get; }
    public string Date { get; set; }
    
    public AuthorAttribute(string name)
    {
        Name = name;
    }
}

// Usage
[Author("John Doe", Date = "2024-01-01")]
public class MyClass
{
    [Author("Jane Doe")]
    public void MyMethod()
    {
    }
}
```

### 3. Reading Attributes (Reflection)

```csharp
// Get attribute from class
var attribute = typeof(MyClass)
    .GetCustomAttribute<AuthorAttribute>();

if (attribute != null)
{
    Console.WriteLine($"Author: {attribute.Name}");
    Console.WriteLine($"Date: {attribute.Date}");
}

// Get attribute from method
var method = typeof(MyClass).GetMethod("MyMethod");
var methodAttr = method.GetCustomAttribute<AuthorAttribute>();
```

---

## 🔬 Reflection

### 1. Type Information

```csharp
// Get type
Type type = typeof(Person);
Type type2 = person.GetType();

// Type properties
string name = type.Name;           // "Person"
string fullName = type.FullName;   // "MyNamespace.Person"
bool isClass = type.IsClass;       // true
bool isInterface = type.IsInterface; // false
```

### 2. Creating Instances

```csharp
// Create instance
Type type = typeof(Person);
object instance = Activator.CreateInstance(type);

// With constructor parameters
object instance2 = Activator.CreateInstance(
    type, 
    new object[] { "John", 30 });

// Cast to specific type
var person = (Person)instance;
```

### 3. Getting Members

```csharp
Type type = typeof(Person);

// Get properties
PropertyInfo[] properties = type.GetProperties();
foreach (var prop in properties)
{
    Console.WriteLine($"{prop.Name}: {prop.PropertyType}");
}

// Get methods
MethodInfo[] methods = type.GetMethods();

// Get fields
FieldInfo[] fields = type.GetFields();

// Get specific property
PropertyInfo nameProp = type.GetProperty("Name");
```

### 4. Invoking Members

```csharp
var person = new Person { Name = "John", Age = 30 };
Type type = person.GetType();

// Get property value
PropertyInfo nameProp = type.GetProperty("Name");
object nameValue = nameProp.GetValue(person);
Console.WriteLine(nameValue); // "John"

// Set property value
nameProp.SetValue(person, "Jane");

// Invoke method
MethodInfo method = type.GetMethod("SayHello");
method.Invoke(person, null);
```

---

## 🔌 Extension Methods

### 1. Creating Extension Methods

```csharp
// Extension methods must be in static class
public static class StringExtensions
{
    // First parameter with 'this' keyword
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
    
    public static string Truncate(this string str, int maxLength)
    {
        if (str == null) return null;
        return str.Length <= maxLength 
            ? str 
            : str.Substring(0, maxLength) + "...";
    }
}

// Usage
string text = "Hello, World!";
bool isEmpty = text.IsNullOrEmpty(); // false
string truncated = text.Truncate(5); // "Hello..."
```

### 2. Common Extension Methods

```csharp
public static class Extensions
{
    // IEnumerable extension
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
    {
        return source == null || !source.Any();
    }
    
    // DateTime extension
    public static bool IsWeekend(this DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday 
            || date.DayOfWeek == DayOfWeek.Sunday;
    }
    
    // Int extension
    public static bool IsEven(this int number)
    {
        return number % 2 == 0;
    }
}

// Usage
var numbers = new List<int> { 1, 2, 3 };
bool isEmpty = numbers.IsNullOrEmpty(); // false

var today = DateTime.Now;
bool isWeekend = today.IsWeekend();

int num = 42;
bool isEven = num.IsEven(); // true
```

---

## 🎯 Pattern Matching

### 1. Type Pattern

```csharp
object obj = "Hello";

// Type pattern
if (obj is string str)
{
    Console.WriteLine(str.ToUpper());
}

// Switch expression
string result = obj switch
{
    string s => s.ToUpper(),
    int i => i.ToString(),
    _ => "Unknown"
};
```

### 2. Property Pattern

```csharp
public record Person(string Name, int Age);

var person = new Person("John", 30);

// Property pattern
if (person is { Age: >= 18 })
{
    Console.WriteLine("Adult");
}

// Switch expression with property pattern
string category = person switch
{
    { Age: < 18 } => "Minor",
    { Age: >= 18 and < 65 } => "Adult",
    { Age: >= 65 } => "Senior",
    _ => "Unknown"
};
```

### 3. Positional Pattern

```csharp
public record Point(int X, int Y);

var point = new Point(0, 0);

// Positional pattern
string location = point switch
{
    (0, 0) => "Origin",
    (var x, 0) => $"On X-axis at {x}",
    (0, var y) => $"On Y-axis at {y}",
    (var x, var y) => $"At ({x}, {y})"
};
```

### 4. Relational Pattern

```csharp
int score = 85;

string grade = score switch
{
    >= 90 => "A",
    >= 80 => "B",
    >= 70 => "C",
    >= 60 => "D",
    _ => "F"
};
```

---

## 📝 Records

### 1. Record Basics

```csharp
// Record declaration
public record Person(string Name, int Age);

// Usage
var person1 = new Person("John", 30);
var person2 = new Person("John", 30);

// Value equality
bool areEqual = person1 == person2; // true

// Immutable by default
// person1.Name = "Jane"; // Error!

// With expression (create copy with changes)
var person3 = person1 with { Age = 31 };
```

### 2. Record vs Class

```csharp
// Record (value-based equality)
public record PersonRecord(string Name, int Age);

// Class (reference-based equality)
public class PersonClass
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// Comparison
var rec1 = new PersonRecord("John", 30);
var rec2 = new PersonRecord("John", 30);
Console.WriteLine(rec1 == rec2); // true (value equality)

var cls1 = new PersonClass { Name = "John", Age = 30 };
var cls2 = new PersonClass { Name = "John", Age = 30 };
Console.WriteLine(cls1 == cls2); // false (reference equality)
```

### 3. Record with Methods

```csharp
public record Person(string Name, int Age)
{
    // Additional properties
    public string FullName { get; init; }
    
    // Methods
    public bool IsAdult() => Age >= 18;
    
    // Override ToString
    public override string ToString() => $"{Name} ({Age})";
}
```

---

## ❓ Nullable Reference Types

### 1. Enabling Nullable Reference Types

```csharp
// In .csproj
<Nullable>enable</Nullable>

// Or in code
#nullable enable
```

### 2. Nullable Annotations

```csharp
// Non-nullable (default)
string name = "John";
// name = null; // Warning!

// Nullable
string? nullableName = null; // OK

// Method with nullable parameter
public void PrintName(string? name)
{
    if (name != null)
    {
        Console.WriteLine(name.ToUpper());
    }
}

// Method returning nullable
public string? FindUser(int id)
{
    return id > 0 ? "John" : null;
}
```

### 3. Null-Forgiving Operator

```csharp
string? nullableStr = GetString();

// Compiler warning: possible null reference
// string upper = nullableStr.ToUpper();

// Tell compiler it's not null (use carefully!)
string upper = nullableStr!.ToUpper();
```

### 4. Null-Coalescing

```csharp
string? name = null;

// Null-coalescing operator
string displayName = name ?? "Unknown";

// Null-coalescing assignment
name ??= "Default";

// Null-conditional operator
int? length = name?.Length;
```

---

## ✅ Best Practices

### 1. Naming Conventions

```csharp
// PascalCase for public members
public class MyClass
{
    public string MyProperty { get; set; }
    public void MyMethod() { }
}

// camelCase for private fields (with underscore prefix)
private string _myField;
private readonly IService _service;

// camelCase for parameters and local variables
public void ProcessData(string userName, int userId)
{
    var result = DoSomething();
}

// UPPER_CASE for constants
public const int MAX_RETRY_COUNT = 3;
```

### 2. SOLID Principles

```csharp
// Single Responsibility Principle
// ✅ Good: Each class has one responsibility
public class UserRepository
{
    public Task<User> GetByIdAsync(Guid id) { }
}

public class EmailService
{
    public Task SendEmailAsync(string to, string subject) { }
}

// ❌ Bad: Class doing too much
public class UserManager
{
    public Task<User> GetByIdAsync(Guid id) { }
    public Task SendEmailAsync(string to, string subject) { }
    public Task LogAsync(string message) { }
}

// Dependency Inversion Principle
// ✅ Good: Depend on abstractions
public class UserService
{
    private readonly IUserRepository _repository;
    
    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }
}

// ❌ Bad: Depend on concrete class
public class UserService
{
    private readonly UserRepository _repository;
    
    public UserService()
    {
        _repository = new UserRepository(); // Tight coupling!
    }
}
```

### 3. Error Handling

```csharp
// ✅ Good: Specific exceptions
public User GetUser(Guid id)
{
    var user = _repository.GetById(id);
    if (user == null)
        throw new NotFoundException($"User {id} not found");
    
    return user;
}

// ❌ Bad: Generic exceptions
public User GetUser(Guid id)
{
    var user = _repository.GetById(id);
    if (user == null)
        throw new Exception("Error"); // Too generic!
    
    return user;
}

// ✅ Good: Don't swallow exceptions
try
{
    DoSomething();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error occurred");
    throw; // Re-throw
}

// ❌ Bad: Swallowing exceptions
try
{
    DoSomething();
}
catch
{
    // Silent failure!
}
```

### 4. Async/Await

```csharp
// ✅ Good: Async all the way
public async Task<List<User>> GetUsersAsync()
{
    return await _repository.GetAllAsync();
}

// ❌ Bad: Blocking async code
public List<User> GetUsers()
{
    return GetUsersAsync().Result; // Deadlock risk!
}

// ✅ Good: ConfigureAwait in libraries
public async Task<string> GetDataAsync()
{
    return await httpClient.GetStringAsync(url)
        .ConfigureAwait(false);
}
```

### 5. Resource Management

```csharp
// ✅ Good: Using statement
public void ProcessFile(string path)
{
    using var stream = File.OpenRead(path);
    // Process stream
} // Automatically disposed

// ❌ Bad: Manual disposal
public void ProcessFile(string path)
{
    var stream = File.OpenRead(path);
    // Process stream
    stream.Dispose(); // Easy to forget!
}
```

### 6. Null Checking

```csharp
// ✅ Good: Null-coalescing
string name = user?.Name ?? "Unknown";

// ✅ Good: Null-conditional
int? length = user?.Name?.Length;

// ✅ Good: Pattern matching
if (user is { Name: not null })
{
    Console.WriteLine(user.Name);
}

// ❌ Bad: Verbose null checking
string name;
if (user != null && user.Name != null)
{
    name = user.Name;
}
else
{
    name = "Unknown";
}
```

---

## 🎓 Summary

### Key Takeaways:

1. **Types**: Understand value vs reference types
2. **Collections**: Use appropriate collection types (List, Dictionary, HashSet)
3. **LINQ**: Master query and method syntax
4. **Async/Await**: Use async all the way, avoid blocking
5. **DI**: Prefer constructor injection, understand lifetimes
6. **Generics**: Create reusable, type-safe code
7. **Exceptions**: Use specific exceptions, don't swallow errors
8. **Patterns**: Use modern C# features (records, pattern matching)
9. **Nullable**: Enable nullable reference types for safety
10. **Best Practices**: Follow SOLID principles, naming conventions

### Resources:

- [Microsoft C# Documentation](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [.NET API Browser](https://docs.microsoft.com/en-us/dotnet/api/)
- [C# Language Specification](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/)

---

**Happy Coding! 🚀**
