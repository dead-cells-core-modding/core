using System.Reflection;
using System.Runtime.CompilerServices;

namespace TestRunner
{
    public class GameProxyJitTest
    {
        [Fact]
        public void AllDeclaredMethodsCanBeJitted()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "GameProxy");
            Assert.NotNull(asm);

            Type[] types;
            try
            {
                types = asm!.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }

            var failures = new List<string>();
            foreach (var type in types)
            {
                if (type.IsGenericTypeDefinition || type.ContainsGenericParameters)
                {
                    continue;
                }

                foreach (var method in type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.DeclaredOnly))
                {
                    if (method.IsAbstract ||
                        (method.Attributes & MethodAttributes.PinvokeImpl) != 0 ||
                        method.GetMethodBody() is null ||
                        method.IsGenericMethodDefinition ||
                        method.ContainsGenericParameters)
                    {
                        continue;
                    }

                    try
                    {
                        RuntimeHelpers.PrepareMethod(method.MethodHandle);
                    }
                    catch (Exception ex)
                    {
                        failures.Add(
                            $"{type.FullName}::{method.Name}: " +
                            $"{ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            Assert.True(failures.Count == 0,
                $"JIT failed for {failures.Count} GameProxy method(s):{Environment.NewLine}" +
                string.Join(Environment.NewLine, failures));
        }
    }
}
