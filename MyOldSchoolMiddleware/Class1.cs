using System;
using System.Collections.Generic;

namespace MyOldSchoolMiddleware {
    using FnApp = Action<IDictionary<string, object>, Action<Exception>, Action<int, IDictionary<string, string>, IObservable<string>>>;

    public class Class1 {
        public static FnApp GetApp() {
            return (env, fault, result) => result(200, new Dictionary<string, string> { { "Content-Type", "text/plain" } }, new Thing("Hello from the old world app"));
        }

        public static FnApp GetMiddleware(FnApp app) {
            return (env, fault, result) => {
                env["oldschoolmiddleware.enabled"] = "true";
                app(env, fault, (status, headers, body) => {
                    headers["x-oldschool"] = "hello\r\nworld";
                    headers["x-oldschool-iobservable"] = typeof(IObservable<>).AssemblyQualifiedName;
                    headers["x-oldschool-iobserver"] = typeof(IObserver<>).AssemblyQualifiedName;
                    result(status, headers, body);
                });
            };
        }

        public class Thing : IObservable<string> {
            private readonly string _text;

            public Thing(string text) {
                _text = text;
            }

            public IDisposable Subscribe(IObserver<string> observer) {
                observer.OnNext(_text);
                observer.OnCompleted();
                return new Disposable();
            }

            public class Disposable : IDisposable {
                public void Dispose() {
                }
            }
        }
    }
}
