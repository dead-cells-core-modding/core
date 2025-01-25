namespace ModCore.Events
{
    public class EventBreakException( Exception ex ) : Exception(null, ex)
    {
    }
}
