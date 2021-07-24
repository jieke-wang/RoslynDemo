using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RoslynDemo
{
    public interface IHelloWord
    {
        void SayHello();
    }
    public class Globals
    {
        public int X;
        public int Y;
    }

    internal class CSharpScriptDemo
    {
        public static void Test1()
        {
            //object result = CSharpScript.EvaluateAsync("1 + 2").Result;
            //Console.WriteLine($"{result.GetType()} {result}");

            //int intResult = CSharpScript.EvaluateAsync<int>("1 + 2").Result;
            //Console.WriteLine(intResult);

            //try
            //{
            //    Console.WriteLine(CSharpScript.EvaluateAsync("2+2").Result);
            //}
            //catch (CompilationErrorException e)
            //{
            //    Console.WriteLine(string.Join(Environment.NewLine, e.Diagnostics));
            //}

            //var result = CSharpScript.EvaluateAsync("System.Net.Dns.GetHostName()", ScriptOptions.Default.WithReferences(typeof(System.Net.Dns).Assembly)).Result;
            //Console.WriteLine(result);

            //var result1 = CSharpScript.EvaluateAsync("Directory.GetCurrentDirectory()", ScriptOptions.Default.WithImports("System.IO")).Result;
            //var result2 = CSharpScript.EvaluateAsync("Sqrt(2)", ScriptOptions.Default.WithImports("System.Math")).Result;

            //var globals = new Globals { X = 1, Y = 2 };
            //Console.WriteLine(CSharpScript.EvaluateAsync<int>("X+Y", globals: globals).Result);

            //var script = CSharpScript.Create<int>("X*Y", globalsType: typeof(Globals));
            //script.Compile();
            //for (int i = 0; i < 10; i++)
            //{
            //    Console.WriteLine((script.RunAsync(new Globals { X = i, Y = i }).Result).ReturnValue);
            //}

            //var script = CSharpScript.Create<int>("X*Y", globalsType: typeof(Globals));
            //ScriptRunner<int> runner = script.CreateDelegate();
            //for (int i = 0; i < 10; i++)
            //{
            //    Console.WriteLine(runner(new Globals { X = i, Y = i }).Result);
            //}

            //var state = CSharpScript.RunAsync<int>("int answer = 42;").Result;
            //foreach (var variable in state.Variables)
            //    Console.WriteLine($"{variable.Name} = {variable.Value} of type {variable.Type}");

            //var script = CSharpScript.Create<int>("int x = 1;").ContinueWith("int y = 2;").ContinueWith("x + y");
            //Console.WriteLine((script.RunAsync().Result).ReturnValue);

            //var state = CSharpScript.RunAsync("int x = 1;").Result;
            //state = state.ContinueWithAsync("int y = 2;").Result;
            //state = state.ContinueWithAsync("x+y").Result;
            //Console.WriteLine(state.ReturnValue);

            //var script = CSharpScript.Create<int>("3");
            //Compilation compilation = script.GetCompilation();
        }

        public static void Test2()
        {
            //MetadataReference[] _ref =
            // DependencyContext.Default.CompileLibraries
            //      .First(cl => cl.Name == "Microsoft.NETCore.App")
            //      .ResolveReferencePaths()
            //      .Select(asm => MetadataReference.CreateFromFile(asm))
            //      .ToArray();

            string helloWordClassCode =
@"using System;
namespace RoslynDemo
{
    public class HelloWord : IHelloWord
    {
        public void SayHello()
        { 
            Console.WriteLine(""HelloWord"");
        } 
    }
}";
            List<MetadataReference> dependencyReferences = new List<MetadataReference>();
            dependencyReferences.Add(MetadataReference.CreateFromFile(typeof(IHelloWord).Assembly.Location)); // 抽象定义类库
            dependencyReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Private.CoreLib").Location)); // 基础类库
            dependencyReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location)); // 控制台类库
            dependencyReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)); // 运行时类库

            string assemblyName = "HelloWord.dll";
            var compilation = CSharpCompilation
                .Create(assemblyName)
                .WithOptions(
                    new CSharpCompilationOptions(
                        Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
                        usings: null,
                        optimizationLevel: OptimizationLevel.Debug, // TODO
                        checkOverflow: false,                       // TODO
                        allowUnsafe: true,                          // TODO
                        platform: Platform.AnyCpu,
                        warningLevel: 4,
                        xmlReferenceResolver: null // don't support XML file references in interactive (permissions & doc comment includes)
                    )
                )
                .AddReferences(dependencyReferences)
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(helloWordClassCode));

            string assemblyFilename = Path.GetFullPath(assemblyName);
            var eResult = compilation.Emit(assemblyFilename);

            if (eResult.Success)
            {
                Assembly assembly = Assembly.UnsafeLoadFrom(assemblyFilename);
                Type type = assembly.GetTypes().FirstOrDefault(x => x.Name == "HelloWord");
                object instance = type.GetConstructor(Array.Empty<Type>()).Invoke(null);
                IHelloWord helloWord = instance as IHelloWord;
                helloWord.SayHello();
            }
            else
            {
                Console.WriteLine(string.Join(Environment.NewLine, eResult.Diagnostics));
            }
        }

        public static void Test3()
        {
            //List<string> names = DependencyContext.Default.CompileLibraries.Select(x => x.Name).ToList();
            //Console.WriteLine(string.Join(Environment.NewLine, names));

            //DependencyContext dependencyContext = DependencyContext.Load(typeof(IHelloWord).Assembly);
            //var resolveReferencePaths = DependencyContext.Load(typeof(IHelloWord).Assembly).CompileLibraries.Select(x => x.ResolveReferencePaths()).ToList();
            //List<string> referencePaths = resolveReferencePaths.SelectMany(x => x).Distinct().ToList();
            //Console.WriteLine(string.Join(Environment.NewLine, referencePaths));

            Console.WriteLine(typeof(object).Assembly.Location);
            Console.WriteLine(typeof(Console).Assembly.Location);
            Console.WriteLine(typeof(void).Assembly.Location);
            Console.WriteLine(typeof(string).Assembly.Location);
            Console.WriteLine(typeof(decimal).Assembly.Location);
            Console.WriteLine(Assembly.Load("System.Runtime").Location);
        }
    }
}

// https://github.com/dotnet/roslyn/wiki/Scripting-API-Samples