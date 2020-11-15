using System;

namespace AspNetCorePureDiApi.Models
{
    public class DisposableDependency
        : IDependency, IDisposable
    {
        private static int _instanceCount;

        private readonly int _id;

        public DisposableDependency()
        {
            _id = ++_instanceCount;
        }

        public void Dispose()
        {
            Console.WriteLine($"{this} disposed");
        }

        public override string ToString()
        {
            return $"{nameof(DisposableDependency)}{_id}";
        }
    }
}