using System;
using System.Collections.Generic;

namespace MyNewAgeMiddleware {
    using FnApp = Action<IDictionary<string, object>, Action<Exception>, Action<int, IDictionary<string, string>, IObservable<string>>>;

    public class Class1 {
        public static FnApp GetApp() {
            return (env, fault, result) => result(200, new Dictionary<string, string> { { "Content-Type", "text/plain" } }, new Thing("Hello from the new age app"));
        }

        public static FnApp GetMiddleware(FnApp app) {
            return (env, fault, result) => {
                env["newagemiddleware.enabled"] = "true";
                app(env, fault, (status, headers, body) => {
                    headers["x-newage"] = "hello";
                    headers["x-newage-iobservable"] = typeof(IObservable<>).AssemblyQualifiedName;
                    headers["x-newage-iobserver"] = typeof(IObserver<>).AssemblyQualifiedName;
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
