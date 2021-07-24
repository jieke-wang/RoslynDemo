using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RoslynDemo
{
    public interface IAdd
    {
        int Add(int x, int y);
    }

    //public class AddImplement : IAdd
    //{
    //    public int Add(int x, int y)
    //    {
    //        return x + y;
    //    }
    //}

    internal class DynamicCompile
    {
        const string dllName = "AddDemo.dll";
        static readonly string dllFilename = Path.GetFullPath(dllName);

        public static void Test1()
        {
            string code =
@"namespace RoslynDemo
{
    public class AddImplement : IAdd
    {
        public int Add(int x, int y)
        {
            return x + y;
        }
    }
}";

            IAdd add = null;
            var eResult = CompileCode(code);
            if (eResult.Success)
            {
                Assembly assembly = Assembly.UnsafeLoadFrom(dllFilename);
                Type type = assembly.GetTypes().FirstOrDefault(x => typeof(IAdd).IsAssignableFrom(x));
                object instance = type.GetConstructor(Array.Empty<Type>()).Invoke(null);
                add = instance as IAdd;
                Console.WriteLine(add.Add(2, 3));
            }
            else
            {
                Console.WriteLine(string.Join(Environment.NewLine, eResult.Diagnostics));
            }

            code =
@"namespace RoslynDemo
{
    public class AddImplement : IAdd
    {
        public int Add(int x, int y)
        {
            return x * y;
        }
    }
}";

            eResult = CompileCode(code); // 这里会报dll占用错误
            if (eResult.Success)
            {
                Assembly assembly = Assembly.UnsafeLoadFrom(dllFilename);
                Type type = assembly.GetTypes().FirstOrDefault(x => typeof(IAdd).IsAssignableFrom(x));
                object instance = type.GetConstructor(Array.Empty<Type>()).Invoke(null);
                add = instance as IAdd;
                Console.WriteLine(add.Add(2, 3));
            }
            else
            {
                Console.WriteLine(string.Join(Environment.NewLine, eResult.Diagnostics));
            }
        }

        static EmitResult CompileCode(string code)
        {
            List<MetadataReference> dependencyReferences = new List<MetadataReference>();
            dependencyReferences.Add(MetadataReference.CreateFromFile(typeof(IAdd).Assembly.Location)); // 抽象定义类库
            dependencyReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Private.CoreLib").Location)); // 基础类库
            dependencyReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location)); // 控制台类库
            dependencyReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)); // 运行时类库

            var compilation = CSharpCompilation
                .Create(dllName)
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
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

            var eResult = compilation.Emit(dllFilename);
            return eResult;
        }
    }

    internal class DynamicCompileV2
    {
        const string dllName = "AddDemo.dll";
        static MemoryStream dllStoreage = new MemoryStream();

        public static void Test1()
        {
            string code =
@"namespace RoslynDemo
{
    public class AddImplement : IAdd
    {
        public int Add(int x, int y)
        {
            return x + y;
        }
    }
}";

            IAdd add = null;
            var eResult = CompileCode(code);
            if (eResult.Success)
            {
                Assembly assembly = Assembly.Load(dllStoreage.ToArray());
                Type type = assembly.GetTypes().FirstOrDefault(x => typeof(IAdd).IsAssignableFrom(x));
                object instance = type.GetConstructor(Array.Empty<Type>()).Invoke(null);
                add = instance as IAdd;
                Console.WriteLine(add.Add(2, 3));
            }
            else
            {
                Console.WriteLine(string.Join(Environment.NewLine, eResult.Diagnostics));
            }

            code =
@"namespace RoslynDemo
{
    public class AddImplement : IAdd
    {
        public int Add(int x, int y)
        {
            return x * y;
        }
    }
}";

            eResult = CompileCode(code); // 这里会报dll占用错误
            if (eResult.Success)
            {
                Assembly assembly = Assembly.Load(dllStoreage.ToArray());
                Type type = assembly.GetTypes().FirstOrDefault(x => typeof(IAdd).IsAssignableFrom(x));
                object instance = type.GetConstructor(Array.Empty<Type>()).Invoke(null);
                add = instance as IAdd;
                Console.WriteLine(add.Add(2, 3));
            }
            else
            {
                Console.WriteLine(string.Join(Environment.NewLine, eResult.Diagnostics));
            }
        }

        static EmitResult CompileCode(string code)
        {
            List<MetadataReference> dependencyReferences = new List<MetadataReference>();
            dependencyReferences.Add(MetadataReference.CreateFromFile(typeof(IAdd).Assembly.Location)); // 抽象定义类库
            dependencyReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Private.CoreLib").Location)); // 基础类库
            dependencyReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location)); // 控制台类库
            dependencyReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)); // 运行时类库

            var compilation = CSharpCompilation
                .Create(dllName)
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
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

            if (dllStoreage.Length > 0) // 重置并清空memory stream
            {
                dllStoreage.SetLength(0);
            }
            var eResult = compilation.Emit(dllStoreage);
            return eResult;
        }
    }
}
