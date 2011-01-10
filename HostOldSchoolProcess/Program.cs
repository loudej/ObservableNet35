using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HostOldSchoolProcess {
    using FnApp = Action<IDictionary<string, object>, Action<Exception>, Action<int, IDictionary<string, string>, IObservable<string>>>;

    class Program {
        static void Main(string[] args) {
            const string newAgeAssembly = @"..\..\..\MyNewAgeMiddleware\bin\debug\MyNewAgeMiddleware.dll";
            const string oldSchoolAssembly = @"..\..\..\MyOldSchoolMiddleware\bin\debug\MyOldSchoolMiddleware.dll";

            //var app1Factory = LoadStaticMethod<FnApp>(newAgeAssembly,
            //    "MyNewAgeMiddleware.Class1", "GetApp");
            //var middleware1Factory = LoadStaticMethod<FnApp, FnApp>(newAgeAssembly,
            //    "MyNewAgeMiddleware.Class1", "GetMiddleware");
            var app2Factory = LoadStaticMethod<FnApp>(oldSchoolAssembly,
                "MyOldSchoolMiddleware.Class1", "GetApp");
            var middleware2Factory = LoadStaticMethod<FnApp, FnApp>(oldSchoolAssembly,
                "MyOldSchoolMiddleware.Class1", "GetMiddleware");

            // a 3.5 process may only load 3.5 assemblies... 
            // to keep the rest of the code identical, we'll just always use the old school factories for all of the combinations below
            var app1Factory = app2Factory;
            var middleware1Factory = middleware2Factory;


            Action<int, IDictionary<string, string>, IObservable<string>> result =
                (status, headers, body) => {
                    Console.WriteLine(string.Format("status {0}", status));
                    Console.WriteLine(headers
                        .SelectMany(kv => (kv.Value ?? "")
                            .Split("\r\n".ToArray(), StringSplitOptions.RemoveEmptyEntries)
                            .Select(value => new { name = kv.Key, value }))
                        .Aggregate("", (agg, hdr) => agg + hdr.name + ": " + hdr.value + "\r\n"));
                    Console.WriteLine();
                    body.Subscribe(new ObserveToConsole());
                };


            var app1 = app1Factory();
            app1(new Dictionary<string, object>(), ex => Console.WriteLine(ex.ToString()), result);

            var app2 = app2Factory();
            app2(new Dictionary<string, object>(), ex => Console.WriteLine(ex.ToString()), result);

            var app3 = middleware2Factory(middleware1Factory(middleware2Factory(app1Factory())));
            app3(new Dictionary<string, object>(), ex => Console.WriteLine(ex.ToString()), result);

            var app4 = middleware1Factory(middleware2Factory(middleware1Factory(app2Factory())));
            app4(new Dictionary<string, object>(), ex => Console.WriteLine(ex.ToString()), result);

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }


        private static Func<T> LoadStaticMethod<T>(string assemblyPath, string typeName, string methodName) {
            return (Func<T>)LoadStaticMethod(assemblyPath, typeName, methodName, typeof(Func<T>));
        }

        private static Func<T1, T> LoadStaticMethod<T1, T>(string assemblyPath, string typeName, string methodName) {
            return (Func<T1, T>)LoadStaticMethod(assemblyPath, typeName, methodName, typeof(Func<T1, T>));
        }

        private static Delegate LoadStaticMethod(string assemblyPath, string typeName, string methodName, Type delegateType) {
            var fullPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, assemblyPath));
            var assembly = Assembly.LoadFile(fullPath);
            var type = assembly.GetType(typeName);
            var method = type.GetMethod(methodName);
            return Delegate.CreateDelegate(delegateType, method);
        }

        private class ObserveToConsole : IObserver<string> {
            public void OnNext(string value) {
                Console.WriteLine(value);
            }

            public void OnError(Exception error) {
                Console.WriteLine(error.ToString());
            }

            public void OnCompleted() {
                Console.WriteLine("----------");
            }
        }
    }
}
