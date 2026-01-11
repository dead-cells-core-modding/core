namespace ModCore.Serialization
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHxbitSerializeCallback
    {
        /// <summary>
        /// Called before <see cref="ModCore.Storage.IHxbitSerializable{TData}.GetData()"/> is called
        /// </summary>
        public void OnBeforeSerializing();
        /// <summary>
        /// Called after <see cref="ModCore.Storage.IHxbitSerializable{TData}.SetData(TData)"/> is called
        /// </summary>
        public void OnAfterDeserializing();
    }
}
