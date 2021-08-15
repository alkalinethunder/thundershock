namespace Thundershock.Core
{
    /// <summary>
    /// Represents a piece of arbitrary data that can be accessed safely across multiple threads.
    /// </summary>
    /// <typeparam name="T">The type of data to store in the concurrent property.</typeparam>
    public sealed class ConcurrentProperty<T>
    {
        private T _value;
        private object _lock = new();

        /// <summary>
        /// Gets or sets the value of the concurrent property.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By default, the value stored in the concurrent property will be uninitialized. This means that
        /// numeric values will be 0, value types will be their default empty value, and reference types will be null.
        /// </para>
        /// <para>
        /// Concurrent properties will lock a mutual exclusion zone on each read and write. This may be slow, but ensures
        /// that the engine won't deadlock when doing multithreaded operations where data must be shared across threads. (For example,
        /// a background task doing heavy work while the game thread shows a progress indicator to the user). DO NOT USE CONCURRENT
        /// PROPERTIES FOR NON-CONCURRENT THINGS!
        /// </para>
        /// </remarks>
        public T Value
        {
            get
            {
                lock (_lock)
                {
                    return _value;
                }
            }
            set
            {
                lock (_lock)
                {
                    _value = value;
                }
            }
        }
        
        /// <summary>
        /// Creates a new instance of the <see cref="ConcurrentProperty{T}"/> class.
        /// </summary>
        public ConcurrentProperty() : this(default) {}
        
        /// <summary>
        /// Creates a new instance of the <see cref="ConcurrentProperty{T}"/> class with a default value.
        /// </summary>
        /// <param name="value">The value to initialize the property with.</param>
        public ConcurrentProperty(T value)
        {
            _value = value;
        }
    }
}