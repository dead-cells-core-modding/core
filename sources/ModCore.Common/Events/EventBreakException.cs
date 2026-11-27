namespace ModCore.Events
{
    /// <summary>
    /// Represents an exception that stops event dispatch.
    /// </summary>
    public class EventBreakException( Exception ex ) : Exception(null, ex)
    {
    }
}
