
# Making IObservable transparently 3.5/4.0 interoperable

This is an example of using a .NET 4.0 generic interface placeholder in .NET 3.5
in a way that automatically upgraded those interfaces, without recompiling, 
when the 3.5 assembly is loaded in a 4.0 app domain.

## Projects

Owin.Net35.csproj

* Creates Owin.dll with a .NET 3.5 target framework
* Contains System.IObservable<> and System.IObserver<> interfaces

Owin.Net40.csproj

* Creates Owin.dll with a .NET 4.0 target framework
* Forwards System.IObservable<> and System.IObserver<> interfaces

MyOldSchoolMiddleware

* Targets .NET 3.5 framework
* References Owin.dll from Owin.Net35.csproj

MyNewAgeMiddleware

* Targets .NET 4.0 framework
* Does not reference Owin.dll

HostOldSchoolProcess

* Targets .NET 3.5 framework
* References Owin.dll from Owin.Net35.csproj
* Loads and uses MyOldSchoolMiddleware
* Typesystem binds all System.IObservable<> to Owin.dll

HostNewAgeProcess

* Targets .NET 4.0 framework
* References Owin.dll from Owin.Net40.csproj
* Loads and uses MyOldSchoolMiddleware and MyNewAgeMiddleware
* Typesystem binds all System.IObservable<> to mscorlib.dll


## Other notes

Owin.dll from Owin.Net40.csproj *only* needed by .NET 4 hosts loading Owin components compiled against .NET 3.5

It is not needed (but does not hurt) if all components are compiled against .NET 4 so
there is a clean way to retire that artifact when 3.5 is considered irrelevent. (Simply stop including it.)


## How this works

*Owin.dll targetting .NET 3.5 contains:*

    namespace System {
        public interface IObservable<out T> {
            IDisposable Subscribe(IObserver<T> observer);
        }
        public interface IObserver<in T> {
            void OnNext(T value);
            void OnError(Exception error);
            void OnCompleted();
        }
    }


*Owin.dll targetting .NET 4.0 contains:*

    using System;
    using System.Runtime.CompilerServices;

    [assembly:TypeForwardedTo(typeof(IObservable<>))]
    [assembly:TypeForwardedTo(typeof(IObserver<>))]


That's it. None of the code knows or cares when this forwarding is in effect. 

The new age process and components assume that everything they talk to understands the .NET 4 iobserv interfaces, 
the old school process and components assume that everything they talk to understands the Owin.dll iobserv interfaces, 
and in a .NET 4 app domain all references to these interfaces are of the CLR type.

You can test this from a 3.5 assembly, which will see either
    typeof(IObservable<>).AssemblyQualifiedName == "System.IObservable`1, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
or
    typeof(IObservable<>).AssemblyQualifiedName == "System.IObservable`1, Owin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
without recompiling based on what domain it's loaded in
 

