using AlgorithmClassLibrary;
using AlgorithmClassLibrary.Algorithms;
using AlgorithmClassLibrary.Algorithms.Factory;

namespace LoadBalancerClassLibrary.Base
{
    public class NotificationBase<T> : NotificationBase where T : class, new()
    {
        protected T This;

        public static implicit operator T(NotificationBase<T> thing) { return thing.This; }

        public NotificationBase(T thing = null)
        {
            This = thing ?? new T();
        }
    }
}
