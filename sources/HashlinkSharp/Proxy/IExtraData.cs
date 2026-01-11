namespace Hashlink.Proxy
{
    internal interface IExtraData
    {
        public T GetOrCreateData<T>(Func<HashlinkObj, object> factory) where T : class;
        public T GetData<T>() where T : class, IExtraDataItem => GetOrCreateData<T>(T.Create);
    }
    public interface IExtraDataItem
    {
        public static abstract object Create(HashlinkObj obj);
    }
}
