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
